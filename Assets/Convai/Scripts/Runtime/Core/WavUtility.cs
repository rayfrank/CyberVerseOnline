using System;
using UnityEngine;

namespace Convai.Scripts.Runtime.Core
{
    public static class WavUtility
    {
        public struct WavHeader
        {
            public int ChunkID;          // "RIFF"
            public int FileSize;         // File size (minus 8 bytes)
            public int RiffType;         // "WAVE"

            public int FmtID;            // "fmt "
            public int FmtSize;          // 16 for PCM
            public short AudioFormat;    // 1 for PCM
            public short NumChannels;    // Mono = 1, Stereo = 2, etc.
            public int SampleRate;       // Samples per second (e.g., 44100)
            public int ByteRate;         // SampleRate * NumChannels * BitsPerSample/8
            public short BlockAlign;     // NumChannels * BitsPerSample/8
            public short BitsPerSample;  // 8 bits = 8, 16 bits = 16, etc.

            public int DataID;           // "data"
            public int DataSize;         // Number of bytes in the data.
        }

        public static bool TryParseWavHeader(byte[] wavBytes, out WavHeader header, out int headerSize)
        {
            header = new WavHeader();
            headerSize = 0;

            if (wavBytes == null || wavBytes.Length < 44)
            {
                throw new ArgumentException($"WAV data is too short to contain a header. Length: {(wavBytes == null ? 0 : wavBytes.Length)} bytes");
            }

            try
            {
                // RIFF chunk
                string riffId = System.Text.Encoding.ASCII.GetString(wavBytes, 0, 4);
                header.ChunkID = BitConverter.ToInt32(wavBytes, 0);
                if (riffId != "RIFF")
                {
                    Debug.LogError($"Invalid WAV header: Expected 'RIFF' but got '{riffId}'");
                    return false;
                }

                header.FileSize = BitConverter.ToInt32(wavBytes, 4);
                Debug.Log($"WAV FileSize from header: {header.FileSize}, Actual bytes: {wavBytes.Length}");

                string waveId = System.Text.Encoding.ASCII.GetString(wavBytes, 8, 4);
                header.RiffType = BitConverter.ToInt32(wavBytes, 8);
                if (waveId != "WAVE")
                {
                    Debug.LogError($"Invalid WAV header: Expected 'WAVE' but got '{waveId}'");
                    return false;
                }

                // fmt sub-chunk
                string fmtId = System.Text.Encoding.ASCII.GetString(wavBytes, 12, 4);
                header.FmtID = BitConverter.ToInt32(wavBytes, 12);
                if (fmtId != "fmt ")
                {
                    Debug.LogWarning($"WAV header: Expected 'fmt ' but got '{fmtId}'. Attempting to find data chunk...");
                    int dataChunkPos = FindChunk(wavBytes, "data", 12);
                    if (dataChunkPos == -1)
                    {
                        Debug.LogError("Could not find 'data' chunk.");
                        return false;
                    }
                    Debug.Log($"Found data chunk at position: {dataChunkPos}");
                    header.NumChannels = 1;
                    header.SampleRate = 44100;
                    header.BitsPerSample = 16;
                    header.DataID = BitConverter.ToInt32(wavBytes, dataChunkPos);
                    header.DataSize = BitConverter.ToInt32(wavBytes, dataChunkPos + 4);
                    headerSize = dataChunkPos + 8;
                    Debug.Log($"Using default format values. Data size: {header.DataSize}, Header size: {headerSize}");
                    return true;
                }

                header.FmtSize = BitConverter.ToInt32(wavBytes, 16);
                header.AudioFormat = BitConverter.ToInt16(wavBytes, 20);
                header.NumChannels = BitConverter.ToInt16(wavBytes, 22);
                header.SampleRate = BitConverter.ToInt32(wavBytes, 24);
                header.ByteRate = BitConverter.ToInt32(wavBytes, 28);
                header.BlockAlign = BitConverter.ToInt16(wavBytes, 32);
                header.BitsPerSample = BitConverter.ToInt16(wavBytes, 34);

                Debug.Log($"Format chunk parsed: Format={header.AudioFormat}, Channels={header.NumChannels}, " +
                         $"Rate={header.SampleRate}, BitsPerSample={header.BitsPerSample}, FmtSize={header.FmtSize}");

                int dataChunkPosition = FindChunk(wavBytes, "data", 36);
                if (dataChunkPosition == -1)
                {
                    dataChunkPosition = FindChunk(wavBytes, "data", 20 + header.FmtSize);
                    if (dataChunkPosition == -1)
                    {
                        Debug.LogError("WAV header: 'data' chunk not found after extensive search.");
                        return false;
                    }
                }

                Debug.Log($"Found data chunk at position: {dataChunkPosition}");
                header.DataID = BitConverter.ToInt32(wavBytes, dataChunkPosition);
                header.DataSize = BitConverter.ToInt32(wavBytes, dataChunkPosition + 4);
                headerSize = dataChunkPosition + 8;

                // Handle invalid size values by using actual data size
                if (header.DataSize <= 0 || header.DataSize > wavBytes.Length - headerSize)
                {
                    Debug.LogWarning($"Invalid data size in header: {header.DataSize}. Using actual data size instead.");
                    header.DataSize = wavBytes.Length - headerSize;
                    Debug.Log($"Adjusted data size to: {header.DataSize} bytes");
                }

                if (header.AudioFormat != 1)
                {
                    Debug.LogError($"Unsupported WAV audio format: {header.AudioFormat}. Only PCM (1) is supported.");
                    return false;
                }

                if (header.BitsPerSample != 16 && header.BitsPerSample != 8)
                {
                    Debug.LogWarning($"Uncommon BitsPerSample: {header.BitsPerSample}. Assuming 16-bit conversion path.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing WAV header: {ex.Message}\nStack trace: {ex.StackTrace}");
                return false;
            }
        }

        private static int FindChunk(byte[] source, string chunkName, int startIndex)
        {
            if (source == null || startIndex < 0 || startIndex >= source.Length)
            {
                Debug.LogError($"Invalid parameters for FindChunk: startIndex={startIndex}, sourceLength={(source?.Length ?? 0)}");
                return -1;
            }

            try
            {
                byte[] chunkBytes = System.Text.Encoding.ASCII.GetBytes(chunkName);

                // Search for the chunk, allowing for alignment padding
                for (int i = startIndex; i <= source.Length - 8; i++) // -8 for chunk header
                {
                    bool match = true;
                    for (int j = 0; j < chunkBytes.Length; j++)
                    {
                        if (source[i + j] != chunkBytes[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        Debug.Log($"Found chunk '{chunkName}' at position {i}");
                        return i;
                    }
                }

                Debug.LogWarning($"Chunk '{chunkName}' not found after position {startIndex}");
                return -1;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in FindChunk: {ex.Message}");
                return -1;
            }
        }

        public static float[] ConvertPcmToFloat(byte[] pcmData, short bitsPerSample, short numChannels)
        {
            if (pcmData == null || pcmData.Length == 0)
            {
                Debug.LogError("PCM data is null or empty");
                return new float[0];
            }

            try
            {
                int samples;
                float[] floatData;

                if (bitsPerSample == 16)
                {
                    if (pcmData.Length % 2 != 0)
                    {
                        Debug.LogWarning($"16-bit PCM data length is not even: {pcmData.Length}. Truncating last byte.");
                        Array.Resize(ref pcmData, pcmData.Length - 1);
                    }

                    samples = pcmData.Length / 2; // 2 bytes per sample for 16-bit
                    floatData = new float[samples];

                    for (int i = 0; i < samples; i++)
                    {
                        short pcmSample = BitConverter.ToInt16(pcmData, i * 2);
                        floatData[i] = pcmSample / 32768f; // Normalize to [-1.0, 1.0]
                    }
                }
                else if (bitsPerSample == 8)
                {
                    samples = pcmData.Length;
                    floatData = new float[samples];

                    for (int i = 0; i < samples; i++)
                    {
                        // 8-bit PCM is unsigned [0, 255], convert to [-1.0, 1.0]
                        floatData[i] = ((pcmData[i] - 128) / 128f);
                    }
                }
                else
                {
                    Debug.LogError($"Unsupported bits per sample: {bitsPerSample}");
                    return new float[0];
                }

                Debug.Log($"Converted {pcmData.Length} bytes of {bitsPerSample}-bit PCM to {floatData.Length} float samples");
                return floatData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting PCM to float: {ex.Message}");
                return new float[0];
            }
        }

        public static float CalculateDurationSeconds(byte[] wavBytes)
        {
            if (TryParseWavHeader(wavBytes, out WavHeader header, out int headerSize))
            {
                // Calculate the total number of samples in the data chunk
                int totalSamples = header.DataSize / (header.NumChannels * (header.BitsPerSample / 8));

                // Calculate the duration in seconds
                return (float)totalSamples / header.SampleRate;
            }

            Debug.LogError("Failed to parse WAV header for duration calculation");
            return 0f;
        }
    }
}