
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PedestalTextureTransfer : UdonSharpBehaviour
{
    public VRC_AvatarPedestal pedestal;
    public CustomRenderTexture crt;
    public string crtTexName = "_Tex";

    public UdonSharpBehaviour OnTextureRead;
    public string OnTextureReadMethodName;

    public Material debugOutput;
    public string debugOutputTexName;

    Material AvatarPedestalGetMaterial(VRC_AvatarPedestal pedestal)
    {
        /* Get last child */
        int lastChildIndex = pedestal.transform.childCount - 1;
        if (lastChildIndex < 0)
        {
            Debug.Log("No children in the Avatar Pedestal. What the panda ?");
            return null;
        }
        var root = pedestal.transform.GetChild(lastChildIndex);
        if (root == null)
        {
            Debug.Log("No Root. Sad panda.");
            return null;
        }

        var image = root.Find("Image");
        if (image == null)
        {
            Debug.Log("No Image. Angry panda !");
            return null;
        }

        return image.GetComponent<MeshRenderer>().sharedMaterial;
    }

    Texture AvatarPedestalGetTexture(VRC_AvatarPedestal pedestal)
    {
        return AvatarPedestalGetMaterial(pedestal).GetTexture("_WorldTex");
    }

    public void Start()
    {
        object[] toCheck = {
            pedestal, crt, crtTexName,
            OnTextureRead, OnTextureReadMethodName,
            debugOutput, debugOutputTexName
        };

        foreach (var o in toCheck)
        {
            if (o == null)
            {
                Debug.LogError($"{name} is not setup correctly !");
                gameObject.SetActive(false);
                return;
            }
        }
    }

    public override void Interact()
    {
        Texture tex = AvatarPedestalGetTexture(pedestal);
        if (tex == null)
        {
            Debug.Log("No texture on the Pedestal !?");
            return;
        }

        debugOutput.SetTexture(debugOutputTexName, tex);
        crt.material.SetTexture(crtTexName, tex);

        OnTextureRead.SendCustomEvent(OnTextureReadMethodName);
    }
}
