using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class WorldListReader : MonoBehaviour
{
    public Texture2D texture;

    private int PixelsToInt(Color32[] colors, int index)
    {
        Color32 color = colors[index];
        return
              (color.r << 0)
            | (color.g << 8)
            | (color.b << 16)
            | (color.a << 24);
    }

    private uint PixelsToUint(Color32[] colors, int index)
    {
        Color32 color = colors[index];
        return (uint)(
              (color.r << 0)
            | (color.g << 8)
            | (color.b << 16)
            | (color.a << 24));
    }

    private ulong PixelsToULong(Color32[] colors, int index)
    {
        ulong low = PixelsToUint(colors, index);
        ulong high = PixelsToUint(colors, index + 1);
        return ((high << 32) | low);
    }

    public static readonly int bytesPerColor = 4;

    private byte[] PixelsToBytes(Color32[] colors, int colorIndex, int arraySize)
    {
        // Ensure that the array size is a multiple of 4
        byte[] bytes = new byte[arraySize];

        for (int i = 0; i < arraySize; i += bytesPerColor, colorIndex++)
        {
            Color32 color = colors[colorIndex];
            bytes[i + 0] = color.r;
            bytes[i + 1] = color.g;
            bytes[i + 2] = color.b;
            bytes[i + 3] = color.a;
        }

        return bytes;
    }

    private string PixelsToString(Color32[] colors, int colorIndex, int maxStringSize)
    {
        return Encoding.UTF8.GetString(PixelsToBytes(colors, colorIndex, maxStringSize));
    }

    public static readonly int XMBF = 0x46424d58;
    public static readonly int EST0 = 0x00545345;
    public static readonly int VOYA = 0x41594f56;
    public static readonly int GE00 = 0x00004547;

    void ReadEntry(Color32[] pixels, int entryIndex)
    {
        int cursor = entryIndex;

        string name = PixelsToString(pixels, entryIndex, 256);
        entryIndex += (256 / bytesPerColor);

        string author = PixelsToString(pixels, entryIndex, 128);
        entryIndex += (128 / bytesPerColor);

        string worldID = PixelsToString(pixels, entryIndex, 64);
        entryIndex += (64 / bytesPerColor);

        byte[] tags = PixelsToBytes(pixels, entryIndex, 32);
        entryIndex += (32 / bytesPerColor);

        ulong creationEpoch = PixelsToULong(pixels, entryIndex);
        entryIndex += (sizeof(ulong) / bytesPerColor);

        ulong lastUpdateEpoch = PixelsToULong(pixels, entryIndex);
        entryIndex += (sizeof(ulong) / bytesPerColor);

        ulong size = PixelsToULong(pixels, entryIndex);

        Debug.Log($"World : {worldID}");
        Debug.Log($"\tName : {name}\n");
        Debug.Log($"\tAuthor : {author}\n");
        Debug.Log($"\tSize : {size}\n");
        Debug.Log($"\tCreated at : {creationEpoch}\n");
        Debug.Log($"\tUpdated at : {lastUpdateEpoch}\n");
        Debug.Log($"\tTags : {tags[0]:X2}");
    }

    // Start is called before the first frame update
    void Start()
    {
        Color32[] pixels = texture.GetPixels32();

        uint magic0 = PixelsToUint(pixels, 0);
        uint magic1 = PixelsToUint(pixels, 1);
        uint magic2 = PixelsToUint(pixels, 2);
        uint magic3 = PixelsToUint(pixels, 3);

        

        bool valid =
            (magic0 == XMBF)
            & (magic1 == EST0)
            & (magic2 == VOYA)
            & (magic3 == GE00);

        if (!valid)
        {
            Debug.Log("Invalid header !");
            Debug.Log($"Got      : 0x{magic0:X8} - 0x{magic1:X8} - 0x{magic2:X8} - 0x{magic3:X8}");
            Debug.Log($"Expected : 0x{XMBF:X8} - 0x{EST0:X8} - 0x{VOYA:X8} - 0x{GE00:X8}");
            return;
        }

        uint version = PixelsToUint(pixels, 4);
        uint entries = PixelsToUint(pixels, 5);
        ulong updated = PixelsToULong(pixels, 6);

        Debug.Log($"Version {version} - {entries} entries - Updated at EPOCH {updated}");

        for (int i = 0; i < entries; i++)
        {
            int cursor = ((512 / bytesPerColor) * (i+1));
            ReadEntry(pixels, cursor);
        }


    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
