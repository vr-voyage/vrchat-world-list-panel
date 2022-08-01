#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.IO;
using System.Text;

namespace Voyage
{
    public static class StreamWriterHelpers
    {
        public static void WriteString(
            this BinaryWriter writer,
            string str,
            int bytesLimit)
        {
            byte[] stringBytes = Encoding.UTF8.GetBytes(str);
            int length = stringBytes.Length;
            if (length > bytesLimit)
            {
                throw new IndexOutOfRangeException(
                    $"String {str} cannot fit in {bytesLimit} bytes ({length} bytes)");
            }

            writer.Write(stringBytes, 0, length);
        }
    }

    [Serializable]
    public class JSONXMBListing
    {
        public string type;
        public int version;
        public JSONXMBEntry[] worlds;
    }

    [Serializable]
    public class JSONXMBEntry
    {
        public string type;
        public string name;
        public string author;
        public string id;
        public string size;
        public string creationDate;
        public string updateDate;
        public string[] tags;
    }

    /* FIXME : Set that as a Component, instead of a floating window.
     * There's almost no need for a floating window anyway, beside testing
     */
    public class ListToTexture : EditorWindow
    {

        SerializedObject serialO;
        SerializedProperty worldListJsonAssetSerialized;
        SerializedProperty pathSerialized;
        public TextAsset worldListJsonAsset;
        public UnityEngine.Object saveDir;
        private string assetsDir;
        public string saveFilePath;

        /* Note : You cannot move this before variables declaration */
        [MenuItem("Voyage / Word listing to texture")]

        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ListToTexture), true);
        }

        private void OnEnable()
        {
            assetsDir = Application.dataPath;
            serialO = new SerializedObject(this);
            worldListJsonAssetSerialized = serialO.FindProperty("worldListJsonAsset");
            pathSerialized = serialO.FindProperty("saveFilePath");

            saveDir = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets");
        }

        const int float_size = 4;
        const int int_size = 4;

        void DumpMetadata(float[] metadata)
        {
            Debug.Log($"{(int)metadata[0]:X} {(int)metadata[1]:X}");
            Debug.Log($"vertices : {metadata[2]}");
            Debug.Log($"normals  : {metadata[3]}");
            Debug.Log($"uvs      : {metadata[4]}");
            Debug.Log($"indices  : {metadata[5]}");
        }

        public byte TAG_PC    = 1;
        public byte TAG_QUEST = 2;

        public const int nameByteSize = 256;
        public const int authorByteSize = 128;
        public const int worldIDSize = 64;

        void WriteEntry(BinaryWriter writer, JSONXMBEntry entry)
        {
            ulong createdAt = ulong.Parse(entry.creationDate);
            ulong updatedAt = ulong.Parse(entry.updateDate);
            ulong byteSize = ulong.Parse(entry.size);

            byte[] tags = new byte[32];
            /* FIXME Get rid of this horror.
             * Use a hash map using "string" : (byteIndex, mask)
             */
            if (Array.IndexOf(entry.tags, "pc") != -1)
            {
                tags[0] |= TAG_PC;
            }
            if (Array.IndexOf(entry.tags, "quest") != -1)
            {
                tags[0] |= TAG_QUEST;
            }

            long cursor = writer.BaseStream.Position;
            writer.WriteString(entry.name, nameByteSize);
            writer.BaseStream.Position = cursor + nameByteSize;

            cursor = writer.BaseStream.Position;
            writer.WriteString(entry.author, authorByteSize);
            writer.BaseStream.Position = cursor + authorByteSize;

            cursor = writer.BaseStream.Position;
            writer.WriteString(entry.id, worldIDSize);
            writer.BaseStream.Position = cursor + worldIDSize;

            cursor = writer.BaseStream.Position;
            writer.Write(tags);
            writer.BaseStream.Position = cursor + 32; // 256 bits

            writer.Write(entry.creationDate);
            writer.Write(entry.updateDate);
            writer.Write(entry.size);

            return;
        }

        Texture2D EncodeListToTexture(JSONXMBListing listing)
        {
            byte[] pixels = new byte[1024 * 1024 * 4];

            MemoryStream stream = new MemoryStream(pixels, 0, pixels.Length, true, true);
            BinaryWriter writer = new BinaryWriter(stream);

            /* uint32[4] magic */
            uint[] magic = new uint[4];
            magic[0] = 0x46424d58; // XMBF
            magic[1] = 0x00545345; // EST0
            magic[2] = 0x41594f56; // VOYA
            magic[3] = 0x00004547; // GE00

            int version = 0;
            int entries = listing.worlds.Length;
            ulong last_updated = 1659368079;

            foreach (uint magicNumber in magic)
            {
                writer.Write(magicNumber);
            }

            writer.Write(version);
            writer.Write(entries);
            writer.Write(last_updated);

            /* Next entry should be aligned on 512 bytes.
             * I hope this works... */
            writer.BaseStream.Position = 512;

            long cursor = writer.BaseStream.Position;
            foreach (var entry in listing.worlds)
            {
                WriteEntry(writer, entry);
                cursor += 512; // Just align on 512 bytes boundary
                writer.BaseStream.Position = cursor;
            }
            //WriteListingEntries(writer, listing.worlds);

            writer.Flush();
            stream.Flush();

            Texture2D texture = new Texture2D(1024, 1024);
            byte[] writtenPixels = stream.GetBuffer();
            texture.SetPixelData(writtenPixels, 0, 0);
            return texture;
        }

        private void OnGUI()
        {


            bool everythingOK = true;
            serialO.Update();

            EditorGUILayout.PropertyField(worldListJsonAssetSerialized, true);
            EditorGUILayout.PropertyField(pathSerialized);
            serialO.ApplyModifiedProperties();

            if (worldListJsonAsset == null || saveFilePath == null || saveFilePath == "") everythingOK = false;
            if (!everythingOK) return;

            if (GUILayout.Button("Generate texture from listing"))
            {
                JSONXMBListing listing = JsonUtility.FromJson<JSONXMBListing>(worldListJsonAsset.text);

                Debug.Log($"listing : {listing.type} version {listing.version}");

                if (listing.worlds == null)
                {
                    Debug.Log("No entries :C");
                    return;
                }
                foreach (var entry in listing.worlds)
                {
                    Debug.Log($"{entry.name} by {entry.author} - {UInt64.Parse(entry.size) / 1024.0} KB");
                }


                Texture2D texture = EncodeListToTexture(listing);

                if (texture == null) return;

                byte[] pngData = texture.EncodeToPNG();
                File.WriteAllBytes($"{assetsDir}/{saveFilePath}", pngData);

                AssetDatabase.Refresh();

            }
        }
    }

}

#endif
