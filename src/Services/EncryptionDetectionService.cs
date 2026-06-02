using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Models;

namespace GeneralUpdate.Tools.Services;

public class EncryptionDetectionService
{
    // ── thresholds ──────────────────────────────────────────
    private const double SectionEntropyThreshold = 7.5;
    private const double FullFileEntropyThreshold = 7.8;

    // ── known encryption-file extensions (high confidence) ──
    private static readonly HashSet<string> EncryptionExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".enc", ".gpg", ".pgp", ".aes", ".crypt", ".locked",
        ".encrypted", ".crypted", ".protected", ".secure",
        ".cryptor", ".locky", ".ransom", ".encfile", ".kraken",
        ".cerber", ".zepto", ".odin", ".thor", ".zzzzz",
        ".exx", ".ezz", ".ecc", ".exyz", ".xyz",
        ".aaa", ".abc", ".xxx", ".ttt", ".micro",
        ".encrypted_data", ".7zenc",
    };

    // ── file extensions to skip for entropy analysis (compressed / media) ──
    private static readonly HashSet<string> EntropySkipExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".7z", ".rar", ".tar", ".gz", ".bz2", ".xz", ".lz", ".lz4", ".zst",
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".avif", ".heic",
        ".mp3", ".aac", ".ogg", ".wav", ".flac", ".opus", ".wma",
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".webm", ".m4v", ".flv",
        ".pdf", ".docx", ".xlsx", ".pptx", ".doc", ".xls", ".ppt",
        ".nupkg", ".snupkg", ".vsix", ".msix", ".appx",
        ".ico", ".cur", ".ani",
    };

    // ── known protector section‑name fingerprints ───────────
    private static readonly HashSet<string> ProtectorSections = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── general packers / compressors ──
        ".mpress1", ".mpress2", "mpress1", "mpress2",
        "UPX0", "UPX1", "UPX2",
        ".upx0", ".upx1", ".upx2",
        "PEC2", "PEC2O", "PEC",

        // ── .NET protectors ──
        ".netshrink",
        ".confuser", "Confuser",
        ".reactor", ".netreactor",
        ".agile", ".agile0", ".agile1",
        ".vmp0", ".vmp1", ".vmp2",
        ".themida", ".winlice",
        ".enigma", ".enigma1", ".enigma2",
        ".sforce", ".sforce0", ".sforce1",
        ".dnguard", ".dnguardhvm",
        ".spices", ".spices0",
        ".netshrink0",
        "ckey",
        "babel", ".babel",
        ".eazfuscator",
        ".sixx",
        ".unpack",
        "CLR", "clr",

        // ── native protectors ──
        ".aspack", ".asprotect",
        ".pelock", ".penguin",
        ".mackt",
        ".petite",
        ".yoda", ".y0da",
        ".obsidium",
        ".armadillo", ".armad",
        ".wwpack", ".wwpack0",
        ".acprotect",
        ".svkp",
        ".taint",
        ".tidec",
        ".nsp0", ".nsp1", ".nsp2",
        ".securom",
        ".safedisc",
    };

    // ── public API ──────────────────────────────────────────

    public async Task<EncryptionScanResult> ScanDirectoryAsync(
        string directoryPath,
        IProgress<int>? progress = null)
    {
        var result = new EncryptionScanResult();

        if (!Directory.Exists(directoryPath))
            return result;

        var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
        result.TotalFilesScanned = files.Length;

        var processed = 0;
        foreach (var filePath in files)
        {
            var relativePath = Path.GetRelativePath(directoryPath, filePath);
            var suspicious = await Task.Run(() => ScanFile(filePath, relativePath)).ConfigureAwait(false);

            if (suspicious != null)
                result.SuspiciousFiles.Add(suspicious);

            processed++;
            progress?.Report(processed * 100 / files.Length);
        }

        // Sort: High → Medium → Low
        result.SuspiciousFiles.Sort((a, b) => b.Level.CompareTo(a.Level));

        return result;
    }

    // ── per‑file scanner dispatcher ─────────────────────────

    private SuspiciousFile? ScanFile(string filePath, string relativePath)
    {
        var ext = Path.GetExtension(filePath);

        // 1. Extension blacklist — highest priority
        var extResult = CheckByExtension(filePath, relativePath, ext);
        if (extResult != null)
            return extResult;

        // 2. PE executables
        if (ext.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".sys", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".pyd", StringComparison.OrdinalIgnoreCase) ||   // Python native extension
            ext.Equals(".node", StringComparison.OrdinalIgnoreCase))    // Node.js native addon
        {
            return CheckPEFile(filePath, relativePath);
        }

        // 3. ELF shared libraries / executables
        if (ext.Equals(".so", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".dylib", StringComparison.OrdinalIgnoreCase) ||
            ext.Length == 0 && IsElfMarker(filePath))       // may be a binary with no ext
        {
            return CheckElfFile(filePath, relativePath);
        }

        // 4. Java / JVM ecosystem
        if (ext.Equals(".jar", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".war", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".ear", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".class", StringComparison.OrdinalIgnoreCase))
        {
            return CheckJarOrClass(filePath, relativePath);
        }

        // 5. Python bytecode
        if (ext.Equals(".pyc", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".pyo", StringComparison.OrdinalIgnoreCase))
        {
            return CheckPycFile(filePath, relativePath);
        }

        // 6. Full‑file entropy (only for data files that aren't in the skip list)
        if (!EntropySkipExtensions.Contains(ext))
        {
            return CheckByEntropy(filePath, relativePath);
        }

        return null;
    }

    // ── detection methods ───────────────────────────────────

    private static SuspiciousFile? CheckByExtension(string filePath, string relativePath, string ext)
    {
        if (EncryptionExtensions.Contains(ext))
        {
            return new SuspiciousFile
            {
                FilePath = filePath,
                RelativePath = relativePath,
                Level = RiskLevel.High,
                Reason = $"Known encrypted file extension ({ext})",
                Method = DetectionMethod.ExtensionBlacklist
            };
        }
        return null;
    }

    private static SuspiciousFile? CheckPEFile(string filePath, string relativePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fs);

            // DOS header
            if (fs.Length < 64) return null;
            var mz = reader.ReadUInt16();
            if (mz != 0x5A4D) // "MZ"
            {
                return new SuspiciousFile
                {
                    FilePath = filePath, RelativePath = relativePath,
                    Level = RiskLevel.High,
                    Reason = "PE file missing MZ header (possibly encrypted/obfuscated)",
                    Method = DetectionMethod.PeClrHeader
                };
            }

            // e_lfanew
            fs.Position = 0x3C;
            var peOffset = reader.ReadInt32();
            if (peOffset <= 0 || peOffset >= fs.Length - 4)
            {
                return new SuspiciousFile
                {
                    FilePath = filePath, RelativePath = relativePath,
                    Level = RiskLevel.High,
                    Reason = "PE signature offset invalid (corrupted or encrypted)",
                    Method = DetectionMethod.PeClrHeader
                };
            }

            // PE signature
            fs.Position = peOffset;
            var peSig = reader.ReadUInt32();
            if (peSig != 0x00004550) // "PE\0\0"
            {
                return new SuspiciousFile
                {
                    FilePath = filePath, RelativePath = relativePath,
                    Level = RiskLevel.High,
                    Reason = "PE signature missing (truncated or encrypted file)",
                    Method = DetectionMethod.PeClrHeader
                };
            }

            // COFF header
            var coffOffset = peOffset + 4;
            fs.Position = coffOffset + 2; // skip Machine
            var sectionCount = reader.ReadUInt16();
            fs.Position = coffOffset + 16;
            var optionalHeaderSize = reader.ReadUInt16();

            // Optional header
            var optOffset = coffOffset + 20;
            fs.Position = optOffset;
            var optMagic = reader.ReadUInt16();
            var isPe32Plus = optMagic == 0x20B;
            var isPe32 = optMagic == 0x10B;

            // Section headers
            var sectionHeadersOffset = optOffset + optionalHeaderSize;

            // ── check section names against protector fingerprints ──
            for (int i = 0; i < sectionCount && i < 64; i++)
            {
                fs.Position = sectionHeadersOffset + i * 40;
                var nameBytes = reader.ReadBytes(8);
                // Trim trailing nulls/spaces
                var name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0', ' ');

                if (!string.IsNullOrEmpty(name) && ProtectorSections.Contains(name))
                {
                    return new SuspiciousFile
                    {
                        FilePath = filePath, RelativePath = relativePath,
                        Level = RiskLevel.High,
                        Reason = $"PE section \"{name}\" matches known protector/packer fingerprint",
                        Method = DetectionMethod.PeProtectorSection,
                        DetectionDetail = $"Section: {name}"
                    };
                }
            }

            // ── .NET assembly? check CLR header (DataDirectory[14]) ──
            int dataDirOffset;
            if (isPe32Plus)
                dataDirOffset = optOffset + 112;
            else if (isPe32)
                dataDirOffset = optOffset + 96;
            else
                dataDirOffset = -1;

            if (dataDirOffset > 0 && dataDirOffset + 14 * 8 + 8 <= fs.Length)
            {
                fs.Position = dataDirOffset + 14 * 8;
                var clrRva = reader.ReadUInt32();
                var clrSize = reader.ReadUInt32();

                if (clrRva != 0 && clrSize != 0)
                {
                    // This is a .NET assembly — CLR header present
                    // Convert RVA to file offset
                    var clrFileOffset = RvaToFileOffset(reader, fs, sectionHeadersOffset, sectionCount, clrRva);

                    if (clrFileOffset > 0 && clrFileOffset < fs.Length)
                    {
                        fs.Position = clrFileOffset;
                        var cb = reader.ReadUInt32(); // CLR header size

                        if (cb < 0x48 || cb > 256)
                        {
                            return new SuspiciousFile
                            {
                                FilePath = filePath, RelativePath = relativePath,
                                Level = RiskLevel.High,
                                Reason = ".NET CLR header size anomaly (possible encryption/stub)",
                                Method = DetectionMethod.PeClrHeader,
                                DetectionDetail = $"CLR cb={cb} (expected 72-256)"
                            };
                        }
                    }
                    else if (clrRva != 0)
                    {
                        // CLR header RVA points to invalid location
                        return new SuspiciousFile
                        {
                            FilePath = filePath, RelativePath = relativePath,
                            Level = RiskLevel.High,
                            Reason = ".NET CLR header points to invalid location (assembly may be encrypted)",
                            Method = DetectionMethod.PeClrHeader
                        };
                    }
                }
            }

            // ── section entropy check: only the first executable section ──
            for (int i = 0; i < sectionCount && i < 64; i++)
            {
                fs.Position = sectionHeadersOffset + i * 40;
                var nameBytes = reader.ReadBytes(8);
                var name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0', ' ');

                // Only check sections that typically contain code
                if (name.Equals(".text", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("CODE", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("text", StringComparison.OrdinalIgnoreCase))
                {
                    fs.Position = sectionHeadersOffset + i * 40 + 8; // VirtualSize
                    var virtualSize = reader.ReadUInt32();
                    fs.Position = sectionHeadersOffset + i * 40 + 20; // PointerToRawData
                    var rawPointer = reader.ReadUInt32();
                    fs.Position = sectionHeadersOffset + i * 40 + 16; // SizeOfRawData
                    var rawSize = reader.ReadUInt32();

                    var sectionDataSize = rawSize > 0 ? rawSize : Math.Min(virtualSize, 1024 * 1024);
                    if (sectionDataSize > 0 && rawPointer > 0 && rawPointer < fs.Length)
                    {
                        var entropy = ComputeStreamEntropy(fs, rawPointer, (int)Math.Min(sectionDataSize, 512 * 1024));
                        if (entropy > SectionEntropyThreshold)
                        {
                            return new SuspiciousFile
                            {
                                FilePath = filePath, RelativePath = relativePath,
                                Level = RiskLevel.Medium,
                                Reason = $"PE code section \"{name}\" has high entropy ({entropy:F2}), possible encryption",
                                Entropy = entropy,
                                Method = DetectionMethod.PeSectionEntropy,
                                DetectionDetail = $"Section: {name}, entropy: {entropy:F2}"
                            };
                        }
                    }
                    break; // Only check the first code section
                }
            }
        }
        catch
        {
            // If we can't read the file, skip it
        }

        return null;
    }

    private static SuspiciousFile? CheckElfFile(string filePath, string relativePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fs);

            if (fs.Length < 64) return null;

            // ELF magic
            var magic = reader.ReadUInt32();
            if (magic != 0x464C457F) // 0x7F 'E' 'L' 'F'
                return null;

            // Class: 1 = 32-bit, 2 = 64-bit
            var elfClass = reader.ReadByte();
            var is64 = elfClass == 2;

            // Section header info
            long shoff;
            ushort shentsize, shnum, shstrndx;

            if (is64)
            {
                fs.Position = 0x28; // e_shoff (8 bytes)
                shoff = reader.ReadInt64();
                fs.Position = 0x3A; // e_shentsize (2 bytes)
                shentsize = reader.ReadUInt16();
                fs.Position = 0x3C; // e_shnum (2 bytes)
                shnum = reader.ReadUInt16();
                fs.Position = 0x3E; // e_shstrndx (2 bytes)
                shstrndx = reader.ReadUInt16();
            }
            else
            {
                fs.Position = 0x20; // e_shoff (4 bytes)
                shoff = reader.ReadUInt32();
                fs.Position = 0x2E; // e_shentsize (2 bytes)
                shentsize = reader.ReadUInt16();
                fs.Position = 0x30; // e_shnum (2 bytes)
                shnum = reader.ReadUInt16();
                fs.Position = 0x32; // e_shstrndx (2 bytes)
                shstrndx = reader.ReadUInt16();
            }

            if (shoff == 0 || shnum == 0 || shentsize == 0)
                return null;

            // Read section name string table first (shstrndx)
            long strTabOffset;
            long strTabSize;
            if (is64)
            {
                strTabOffset = shoff + (long)shstrndx * shentsize + 0x18; // sh_offset at +24
                fs.Position = strTabOffset;
                strTabOffset = reader.ReadInt64();
                fs.Position = shoff + (long)shstrndx * shentsize + 0x20; // sh_size at +32
                strTabSize = reader.ReadInt64();
            }
            else
            {
                strTabOffset = shoff + (long)shstrndx * shentsize + 0x10; // sh_offset at +16
                fs.Position = strTabOffset;
                strTabOffset = reader.ReadUInt32();
                fs.Position = shoff + (long)shstrndx * shentsize + 0x14; // sh_size at +20
                strTabSize = reader.ReadUInt32();
            }

            if (strTabOffset <= 0 || strTabSize <= 0 || strTabOffset >= fs.Length)
                return null;

            // Iterate sections
            for (int i = 0; i < shnum && i < 256; i++)
            {
                long sectionOffset = shoff + (long)i * shentsize;

                long shNameIndex, shOffset, shSize;
                if (is64)
                {
                    fs.Position = sectionOffset; // sh_name
                    shNameIndex = reader.ReadUInt32();
                    fs.Position = sectionOffset + 0x18; // sh_offset
                    shOffset = reader.ReadInt64();
                    fs.Position = sectionOffset + 0x20; // sh_size
                    shSize = reader.ReadInt64();
                }
                else
                {
                    fs.Position = sectionOffset; // sh_name
                    shNameIndex = reader.ReadUInt32();
                    fs.Position = sectionOffset + 0x10; // sh_offset
                    shOffset = reader.ReadUInt32();
                    fs.Position = sectionOffset + 0x14; // sh_size
                    shSize = reader.ReadUInt32();
                }

                // Read section name from string table
                var name = ReadElfString(fs, strTabOffset, strTabSize, shNameIndex);

                // Check protector section names
                if (!string.IsNullOrEmpty(name) && ProtectorSections.Contains(name))
                {
                    return new SuspiciousFile
                    {
                        FilePath = filePath, RelativePath = relativePath,
                        Level = RiskLevel.High,
                        Reason = $"ELF section \"{name}\" matches known protector/packer fingerprint",
                        Method = DetectionMethod.ElfSectionEntropy,
                        DetectionDetail = $"Section: {name}"
                    };
                }

                // Entropy check for .text section
                if (name == ".text" && shOffset > 0 && shSize > 0)
                {
                    var sampleSize = (int)Math.Min(shSize, 512 * 1024);
                    var entropy = ComputeStreamEntropy(fs, shOffset, sampleSize);
                    if (entropy > SectionEntropyThreshold)
                    {
                        return new SuspiciousFile
                        {
                            FilePath = filePath, RelativePath = relativePath,
                            Level = RiskLevel.Medium,
                            Reason = $"ELF .text section has high entropy ({entropy:F2}), possible encryption",
                            Entropy = entropy,
                            Method = DetectionMethod.ElfSectionEntropy,
                            DetectionDetail = $"Section: .text, entropy: {entropy:F2}"
                        };
                    }
                }
            }
        }
        catch
        {
            // Skip on error
        }

        return null;
    }

    private static SuspiciousFile? CheckJarOrClass(string filePath, string relativePath)
    {
        try
        {
            var ext = Path.GetExtension(filePath);

            if (ext.Equals(".class", StringComparison.OrdinalIgnoreCase))
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (fs.Length < 4) return null;
                var magic = new byte[4];
                fs.ReadExactly(magic, 0, 4);
                var magicVal = (uint)(magic[0] << 24 | magic[1] << 16 | magic[2] << 8 | magic[3]);
                if (magicVal != 0xCAFEBABE)
                {
                    return new SuspiciousFile
                    {
                        FilePath = filePath, RelativePath = relativePath,
                        Level = RiskLevel.High,
                        Reason = "Java .class file has invalid magic number (possibly encrypted)",
                        Method = DetectionMethod.JarClassMagic
                    };
                }
            }
            else
            {
                // .jar / .war / .ear — peek inside as ZIP
                try
                {
                    using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.Name.EndsWith(".class", StringComparison.OrdinalIgnoreCase) &&
                            entry.Length >= 4)
                        {
                            using var stream = entry.Open();
                            var magic = new byte[4];
                            stream.ReadExactly(magic, 0, 4);
                            var magicVal = (uint)(magic[0] << 24 | magic[1] << 16 | magic[2] << 8 | magic[3]);
                            if (magicVal != 0xCAFEBABE)
                            {
                                return new SuspiciousFile
                                {
                                    FilePath = filePath, RelativePath = relativePath,
                                    Level = RiskLevel.High,
                                    Reason = $"JAR entry \"{entry.Name}\" has invalid .class magic (possible encryption)",
                                    Method = DetectionMethod.JarClassMagic
                                };
                            }
                            break; // Check first class file only
                        }
                    }
                }
                catch (InvalidDataException)
                {
                    // ZIP structure corrupted — could be encrypted JAR
                    return new SuspiciousFile
                    {
                        FilePath = filePath, RelativePath = relativePath,
                        Level = RiskLevel.Medium,
                        Reason = "JAR/ZIP structure invalid (possibly encrypted or corrupted)",
                        Method = DetectionMethod.JarClassMagic
                    };
                }
            }
        }
        catch
        {
            // Skip on error
        }

        return null;
    }

    private static SuspiciousFile? CheckPycFile(string filePath, string relativePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 4) return null;
            var magic = new byte[4];
            fs.ReadExactly(magic, 0, 4);

            // Python .pyc magic numbers vary by version but are well-known
            // CPython 3.x magic numbers (first 2 bytes as little-endian):
            // 3.0-3.12: range roughly 0x0A0D to 0xCB0D
            var magic16 = (ushort)(magic[0] | (magic[1] << 8));
            bool validPyc = (magic16 >= 0x0A0D && magic16 <= 0xCB0D) || // Python 3.0 - 3.12
                            (magic16 >= 0xD10D && magic16 <= 0xF30D);   // Python 3.13+
            if (!validPyc)
            {
                return new SuspiciousFile
                {
                    FilePath = filePath, RelativePath = relativePath,
                    Level = RiskLevel.Medium,
                    Reason = "Python .pyc file has unrecognized magic number (possibly encrypted)",
                    Method = DetectionMethod.FullFileEntropy,
                    DetectionDetail = $"Magic: 0x{magic16:X4}"
                };
            }
        }
        catch { /* skip on error */ }
        return null;
    }

    private static SuspiciousFile? CheckByEntropy(string filePath, string relativePath)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            long fileSize;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileSize = fs.Length;
            }

            var sampleSize = (int)Math.Min(fileSize, 512 * 1024);
            double entropy;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                entropy = ComputeStreamEntropy(fs, 0, sampleSize);
            }

            if (entropy > FullFileEntropyThreshold)
            {
                return new SuspiciousFile
                {
                    FilePath = filePath, RelativePath = relativePath,
                    Level = RiskLevel.Medium,
                    Reason = $"File has very high entropy ({entropy:F2}), likely encrypted or compressed",
                    Entropy = entropy,
                    Method = DetectionMethod.FullFileEntropy
                };
            }
        }
        catch { /* skip on error */ }

        return null;
    }

    // ── helpers ─────────────────────────────────────────────

    /// <summary>Compute Shannon entropy for a byte range of a stream.</summary>
    internal static double ComputeStreamEntropy(Stream stream, long offset, int length)
    {
        var counts = new long[256];
        long total = 0;

        var savedPos = stream.Position;
        stream.Position = offset;

        var buffer = new byte[Math.Min(length, 65536)];
        int remaining = length;

        while (remaining > 0)
        {
            int toRead = Math.Min(remaining, buffer.Length);
            int read;
            try
            {
                stream.ReadExactly(buffer, 0, toRead);
                read = toRead;
            }
            catch (EndOfStreamException)
            {
                break;
            }

            for (int i = 0; i < read; i++)
                counts[buffer[i]]++;

            total += read;
            remaining -= read;
        }

        stream.Position = savedPos;

        if (total == 0) return 0;

        double entropy = 0;
        for (int i = 0; i < 256; i++)
        {
            if (counts[i] == 0) continue;
            var p = (double)counts[i] / total;
            entropy -= p * Math.Log2(p);
        }

        return entropy;
    }

    /// <summary>Convert an RVA to a file offset using PE section headers.</summary>
    private static long RvaToFileOffset(BinaryReader reader, Stream fs,
        long sectionHeadersOffset, int sectionCount, uint rva)
    {
        for (int i = 0; i < sectionCount && i < 64; i++)
        {
            fs.Position = sectionHeadersOffset + i * 40 + 12; // VirtualAddress
            var sectionRva = reader.ReadUInt32();
            fs.Position = sectionHeadersOffset + i * 40 + 8;  // VirtualSize
            var virtualSize = reader.ReadUInt32();
            fs.Position = sectionHeadersOffset + i * 40 + 20; // PointerToRawData
            var rawPointer = reader.ReadUInt32();

            if (rva >= sectionRva && rva < sectionRva + Math.Max(virtualSize, 1))
            {
                return rawPointer + (rva - sectionRva);
            }
        }

        return -1;
    }

    /// <summary>Read an ELF string from the section name string table.</summary>
    private static string ReadElfString(Stream fs, long strTabOffset, long strTabSize, long nameIndex)
    {
        if (nameIndex < 0 || nameIndex >= strTabSize) return "";

        var savedPos = fs.Position;
        fs.Position = strTabOffset + nameIndex;

        var sb = new StringBuilder();
        for (int i = 0; i < 256 && fs.Position < fs.Length; i++)
        {
            var b = fs.ReadByte();
            if (b <= 0) break;
            sb.Append((char)b);
        }

        fs.Position = savedPos;
        return sb.ToString();
    }

    /// <summary>Quick ELF magic check to detect ELF files without extensions.</summary>
    private static bool IsElfMarker(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 4) return false;
            var magic = new byte[4];
            fs.ReadExactly(magic, 0, 4);
            return magic[0] == 0x7F && magic[1] == 'E' && magic[2] == 'L' && magic[3] == 'F';
        }
        catch { return false; }
    }
}

