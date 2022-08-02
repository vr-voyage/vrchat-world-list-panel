
using System.Globalization;
using System.Text;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WorldListReader_Udon : UdonSharpBehaviour
{
    public const int XMBF = 0x46424d58;
    public const int EST0 = 0x00545345;
    public const int VOYA = 0x41594f56;
    public const int GE00 = 0x00004547;

    public const int nameByteSize = 256;
    public const int authorByteSize = 128;
    public const int worldIDSize = 64;
    public const int tagsByteSize = 32;

    public const int char_size = 2;
    public const int ulong_size = 8;

    public Texture2D outputTexture;
    public CustomRenderTexture crt;
    public float updateRateInSeconds;

    float nextUpdateAtSeconds;
    Rect readZone;
    bool generated = false;
    bool initialized = false;

    public TextMeshProUGUI uiText;

    string toShow = "";

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

    public const int bytesPerColor = 4;

    private byte[] PixelsToBytes(Color32[] colors, int colorIndex, int arraySize)
    {
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


    private string UTF16PixelsToString(
        Color32[] colors, int colorIndex,
        int maxStringByteSize)
    {
        int maxChars = maxStringByteSize / char_size;

        char[] chars = new char[maxChars];

        for (int c = 0; c < maxChars; c += 2, colorIndex++)
        {
            Color32 color = colors[colorIndex];
            chars[c + 0] = (char)((color.r << 0) | (color.g << 8));
            chars[c + 1] = (char)((color.b << 0) | (color.a << 8));
        }

        return new string(chars).Replace("\0", "");
    }

    private string ASCIIPixelsToString(
        Color32[] colors, int colorIndex,
        int maxStringByteSize)
    {
        // Ensure that the array size is a multiple of 4
        char[] convertedChars = new char[maxStringByteSize];

        for (int i = 0; i < maxStringByteSize; i += bytesPerColor, colorIndex++)
        {
            Color32 color = colors[colorIndex];
            convertedChars[i + 0] = (char)color.r;
            convertedChars[i + 1] = (char)color.g;
            convertedChars[i + 2] = (char)color.b;
            convertedChars[i + 3] = (char)color.a;
        }

        return new string(convertedChars).Replace("\0", "");
    }

    void Log(string text)
    {
        Debug.Log(text);
        uiText.text += (text.Replace("\0", ""));
    }
    void ReadEntry(Color32[] pixels, int entryIndex)
    {

        string name = UTF16PixelsToString(pixels, entryIndex, nameByteSize);
        entryIndex += (nameByteSize / bytesPerColor);

        string author = UTF16PixelsToString(pixels, entryIndex, authorByteSize);
        entryIndex += (authorByteSize / bytesPerColor);

        string worldID = ASCIIPixelsToString(pixels, entryIndex, worldIDSize);
        entryIndex += (worldIDSize / bytesPerColor);

        byte[] tags = PixelsToBytes(pixels, entryIndex, tagsByteSize);
        entryIndex += (tagsByteSize / bytesPerColor);

        ulong creationEpoch = PixelsToULong(pixels, entryIndex);
        entryIndex += (ulong_size / bytesPerColor);

        ulong lastUpdateEpoch = PixelsToULong(pixels, entryIndex);
        entryIndex += (ulong_size / bytesPerColor);

        ulong size = PixelsToULong(pixels, entryIndex);

        Log($"World : {worldID}\n");
        Log($"    Name : {name}\n");
        Log($"    Author : {author}\n");
        Log($"    Size : {size}\n");
        Log($"    Created at : {creationEpoch}\n");
        Log($"    Updated at : {lastUpdateEpoch}\n");
        Log($"    Tags : {tags[0]:X2}\n");
    }

    // Start is called before the first frame update
    bool GetDataFromTexture(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();

        if (pixels.Length < (1024 / bytesPerColor))
        {
            Debug.LogError($"Not enough data in the texture (Only {pixels.Length*4} bytes)");
            return false;
        }



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
            return false;
        }

        Debug.Log("The magic is correct !");

        uint version = PixelsToUint(pixels, 4);
        uint entries = PixelsToUint(pixels, 5);
        ulong updated = PixelsToULong(pixels, 6);

        uiText.text = "";

        Log($"Version {version} - {entries} entries - Updated at EPOCH {updated}\n");

        for (int i = 0; i < entries; i++)
        {
            int cursor = ((512 / bytesPerColor) * (i + 1));
            ReadEntry(pixels, cursor);
        }

        return true;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (outputTexture == null || crt == null || uiText == null)
        {
            Debug.LogError($"Component not setup correctly on {gameObject.name} !");
            gameObject.SetActive(false);
        }
        crt.Initialize();
        initialized = true;

        /*var renderTexture = GetComponent<Camera>().targetTexture;
        renderTexture.width = outputTexture.width;
        renderTexture.height = outputTexture.height;*/

        readZone = new Rect(0, 0, outputTexture.width, outputTexture.height);

        nextUpdateAtSeconds = Time.time + 0.5f;



    }

    public void StartRendering()
    {
        var camera = GetComponent<Camera>();
        if (camera == null)
        {
            Debug.Log("There MUST be a camera attached to this script !");
            gameObject.SetActive(false);
            return;
        }

        nextUpdateAtSeconds += 0.2f;
        camera.enabled = true;
    }

    private void OnPostRender()
    {
        if (generated)
        {
            Debug.Log("Nothing to do anymore");
            gameObject.SetActive(false);
        }

        float currentTime = Time.time;
        if (initialized)
        {
            initialized = false;
            nextUpdateAtSeconds = currentTime + 0.5f;
            return;
        }

        if (currentTime < nextUpdateAtSeconds) return;
        nextUpdateAtSeconds += currentTime + updateRateInSeconds;

        outputTexture.ReadPixels(readZone, 0, 0, true);
        //outputTexture.Apply();

        if (GetDataFromTexture(outputTexture))
        {
            generated = true;
            Debug.Log("Success !");

        }

        
    }

}
