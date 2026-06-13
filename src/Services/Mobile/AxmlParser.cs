using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace GeneralUpdate.Tools.Services.Mobile;

public class AxmlParseResult
{
    public bool Success { get; init; }
    public string? PackageName { get; init; }
    public string? VersionName { get; init; }
    public string? VersionCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static AxmlParseResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Parses AndroidManifest.xml from AXML (Android Binary XML) format.
/// Extracts package, versionName, and versionCode by locating string pool entries.
///
/// APK path: AndroidManifest.xml (root)
/// AAB  path: base/manifest/AndroidManifest.xml
///
/// AXML chunk structure reference:
///   https://justanapplication.wordpress.com/category/android/android-binary-xml/
/// </summary>
public class AxmlParser
{
    /// <summary>
    /// Parse AndroidManifest.xml from a ZIP archive (APK or AAB).
    /// </summary>
    /// <param name="zipPath">Path to the APK/AAB file.</param>
    /// <param name="entryPath">Entry path inside ZIP: "AndroidManifest.xml" or "base/manifest/AndroidManifest.xml".</param>
    public AxmlParseResult ParseFromZip(string zipPath, string entryPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.GetEntry(entryPath);
            if (entry == null)
                return AxmlParseResult.Fail($"AndroidManifest.xml not found at '{entryPath}'.");

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var data = ms.ToArray();

            return Parse(data);
        }
        catch (InvalidDataException ex)
        {
            return AxmlParseResult.Fail($"Invalid ZIP file: {ex.Message}");
        }
        catch (Exception ex) when (ex is not IOException)
        {
            return AxmlParseResult.Fail($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse raw AXML bytes.
    /// </summary>
    public AxmlParseResult Parse(byte[] data)
    {
        try
        {
            var strings = ExtractStringPool(data);
            if (strings == null || strings.Length == 0)
                return AxmlParseResult.Fail("Could not locate string pool in AndroidManifest.xml.");

            var package = ExtractAttributeString(data, strings, "package");
            var versionName = ExtractAttributeString(data, strings, "versionName");
            var versionCode = ExtractVersionCode(data);

            if (package == null && versionName == null && versionCode == null)
                return AxmlParseResult.Fail("Could not extract any metadata from AndroidManifest.xml.");

            return new AxmlParseResult
            {
                Success = true,
                PackageName = package,
                VersionName = versionName,
                VersionCode = versionCode?.ToString()
            };
        }
        catch (Exception ex)
        {
            return AxmlParseResult.Fail($"AXML parse error: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract string pool from AXML binary.
    /// AXML format: [header(8B)] [StringPool chunk] [XML body chunks]
    /// StringPool chunk: type(2B)=0x0001 + headerSize(2B) + chunkSize(4B) + ...
    /// </summary>
    private static string[]? ExtractStringPool(byte[] data)
    {
        int pos = 8; // skip full header

        while (pos + 8 <= data.Length)
        {
            var chunkType = ReadU16(data, pos);
            // var headerSize = ReadU16(data, pos + 2);
            var chunkSize = ReadU32(data, pos + 4);

            if (pos + chunkSize > data.Length)
                break;

            // StringPool chunk type = 0x0001
            if (chunkType == 0x0001)
                return ParseStringPool(data, pos, (int)chunkSize);

            pos += (int)chunkSize;
        }

        return null;
    }

    private static string[] ParseStringPool(byte[] data, int start, int chunkSize)
    {
        // StringPool header layout (starting at 'start'):
        //   0: chunkType (2B) = 0x0001
        //   2: headerSize (2B) = 0x001C (28 bytes)
        //   4: chunkSize (4B)
        //   8: stringCount (4B)
        //  12: styleCount (4B)
        //  16: flags (4B)
        //  20: stringsStart (4B) — offset from chunk start
        //  24: stylesStart (4B)
        //  28: string offsets array (stringCount * 4B)
        //      ... string data follows at stringsStart

        var stringCount = ReadU32(data, start + 8);
        var stringsStart = ReadU32(data, start + 20);

        if (stringCount == 0 || stringsStart == 0)
            return [];

        var result = new string[stringCount];
        // Read UTF-16LE strings (bit 0 of flags indicates UTF-8 vs UTF-16, but Android uses UTF-16)
        // Each offset points to a uint16 length followed by the string chars

        for (int i = 0; i < stringCount; i++)
        {
            var offset = ReadU32(data, start + 28 + i * 4);
            var stringPos = start + (int)stringsStart + (int)offset;

            if (stringPos + 2 > data.Length)
                continue;

            // String length in characters (stored as uint16)
            var charLen = ReadU16(data, stringPos);
            // Sometimes there's a 2-byte padding before the string; skip if 0
            if (charLen == 0)
            {
                // Could be a 4-byte length field instead
                charLen = ReadU16(data, stringPos + 2);
                stringPos += 2;
            }
            stringPos += 2;

            if (stringPos + charLen * 2 > data.Length || charLen > 1024)
                continue;

            result[i] = Encoding.Unicode.GetString(data, stringPos, charLen * 2).TrimEnd('\0');
        }

        return result;
    }

    /// <summary>
    /// Find an attribute value by scanning for its UTF-8 attribute name in the XML body,
    /// then resolving the string resource ID from the attribute block.
    /// </summary>
    private static string? ExtractAttributeString(byte[] data, string[] strings, string attributeName)
    {
        var nameBytes = Encoding.UTF8.GetBytes(attributeName);

        // Find the attribute name in the string pool first
        int nameIndex = -1;
        for (int i = 0; i < strings.Length; i++)
        {
            if (string.Equals(strings[i], attributeName, StringComparison.Ordinal))
            {
                nameIndex = i;
                break;
            }
        }

        if (nameIndex < 0) return null;

        // Scan through the XML for an attribute with this name index.
        // Start element chunks have type 0x01011002 (starts with 0x00100102 in little-endian)
        int pos = 8;
        while (pos + 8 <= data.Length)
        {
            var chunkType = ReadU16(data, pos);
            var chunkSize = ReadU32(data, pos + 4);

            if (pos + chunkSize > data.Length) break;

            // Start element chunk: type 0x0102
            if (chunkType == 0x0102 && chunkSize >= 40)
            {
                // StartElement layout:
                //   0:  chunkType (2B) = 0x0102
                //   2:  headerSize (2B)
                //   4:  chunkSize (4B)
                //   8:  lineNumber (4B)
                //  12:  commentIndex (4B)
                //  16:  namespaceIndex (4B)
                //  20:  nameIndex (4B) — element name
                //  24:  attributeStart (2B) — offset from chunk start
                //  26:  attributeSize (2B) — each attribute entry size (typically 0x14 = 20)
                //  28:  attributeCount (2B)
                //  30:  idIndex (2B)
                //  32:  classIndex (2B)
                //  34:  styleIndex (2B)
                //  36+: attribute entries (attributeCount * attributeSize bytes)

                var attrStart = ReadU16(data, pos + 24);
                var attrSize = ReadU16(data, pos + 26);
                var attrCount = ReadU16(data, pos + 28);

                // Also check if this is the <manifest> element by name
                // var elementNameIndex = ReadS32(data, pos + 20);

                for (int a = 0; a < attrCount; a++)
                {
                    var attrPos = pos + attrStart + a * attrSize;

                    // Attribute entry layout (20 bytes each):
                    //   0:  namespaceIndex (4B)
                    //   4:  nameIndex (4B)
                    //   8:  rawValueIndex (4B) — index into string pool for string values
                    //  12:  typedValueSize (2B)
                    //  14:  typedValueRes0 (1B)
                    //  15:  typedValueType (1B) — 0x03 = STRING, 0x10 = INT
                    //  16:  typedValueData (4B)

                    var attrNameIndex = ReadS32(data, attrPos + 4);

                    if (attrNameIndex == nameIndex)
                    {
                        var rawValueIndex = ReadS32(data, attrPos + 8);
                        if (rawValueIndex >= 0 && rawValueIndex < strings.Length)
                            return strings[rawValueIndex];
                    }
                }
            }

            pos += (int)chunkSize;
        }

        return null;
    }

    /// <summary>
    /// Extract android:versionCode which is stored as a typed integer value (type 0x10).
    /// </summary>
    private static int? ExtractVersionCode(byte[] data)
    {
        // Find "versionCode" in string pool
        int nameIndex = -1;
        try
        {
            var strings = ExtractStringPool(data);
            if (strings == null) return null;

            for (int i = 0; i < strings.Length; i++)
            {
                if (string.Equals(strings[i], "versionCode", StringComparison.Ordinal))
                {
                    nameIndex = i;
                    break;
                }
            }
            if (nameIndex < 0) return null;

            // Scan start elements for an attribute referencing versionCode
            int pos = 8;
            while (pos + 8 <= data.Length)
            {
                var chunkType = ReadU16(data, pos);
                var chunkSize = ReadU32(data, pos + 4);
                if (pos + chunkSize > data.Length) break;

                if (chunkType == 0x0102 && chunkSize >= 40)
                {
                    var attrStart = ReadU16(data, pos + 24);
                    var attrSize = ReadU16(data, pos + 26);
                    var attrCount = ReadU16(data, pos + 28);

                    for (int a = 0; a < attrCount; a++)
                    {
                        var attrPos = pos + attrStart + a * attrSize;
                        var attrNameIndex = ReadS32(data, attrPos + 4);

                        if (attrNameIndex == nameIndex)
                        {
                            var typedValueType = data[attrPos + 15];
                            // 0x10 = INT_DEC, 0x11 = INT_HEX
                            if (typedValueType == 0x10 || typedValueType == 0x11)
                            {
                                return ReadS32(data, attrPos + 16);
                            }
                        }
                    }
                }

                pos += (int)chunkSize;
            }
        }
        catch
        {
            // Best effort
        }

        return null;
    }

    private static ushort ReadU16(byte[] data, int offset)
    {
        if (offset + 2 > data.Length) return 0;
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }

    private static uint ReadU32(byte[] data, int offset)
    {
        if (offset + 4 > data.Length) return 0;
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    private static int ReadS32(byte[] data, int offset)
    {
        if (offset + 4 > data.Length) return -1;
        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
    }
}
