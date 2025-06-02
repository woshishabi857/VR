using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;

public class PPTProjector : MonoBehaviour
{
    [Tooltip("Material used for displaying the PPT")]
    public Material projectionMaterial;

    [Tooltip("Target object to project the PPT (e.g., whiteboard or screen)")]
    public Renderer targetRenderer;

    [Tooltip("Texture property name in the material (usually _MainTex)")]
    public string texturePropertyName = "_MainTex";

    [Tooltip("Folder containing PPT images (relative to StreamingAssets)")]
    public string pptFolderPath = "PPTSlides";
    
    [Tooltip("Whether to preserve the original aspect ratio of slides")]
    public bool preserveAspectRatio = true;
    
    [Tooltip("Scale factor for the projected image (1.0 = full size)")]
    [Range(0.1f, 2.0f)]
    public float slideScale = 1.0f;

    [Tooltip("是否调整白板/屏幕尺寸以匹配PPT比例")]
    public bool resizeScreen = true;

    [Tooltip("调整屏幕尺寸时保持的参考维度")]
    public enum ResizeDimension { Width, Height, Auto }
    public ResizeDimension resizeReference = ResizeDimension.Auto;

    private List<Texture2D> pptSlides = new List<Texture2D>();
    private int currentSlideIndex = 0;
    private Material instanceMaterial;
    private Vector3 originalScale;

    void Start()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                Debug.LogError("请指定目标渲染器(白板或屏幕)");
                return;
            }
        }

        // 保存原始尺寸
        originalScale = targetRenderer.transform.localScale;

        // 创建材质实例，避免修改原始材质
        if (projectionMaterial == null)
        {
            instanceMaterial = new Material(targetRenderer.sharedMaterial);
        }
        else
        {
            instanceMaterial = new Material(projectionMaterial);
        }
        
        // 应用材质
        targetRenderer.material = instanceMaterial;
        
        StartCoroutine(LoadPPTSlides());
    }

    IEnumerator LoadPPTSlides()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, pptFolderPath);
        if (!Directory.Exists(fullPath))
        {
            Debug.LogWarning("PPT文件夹不存在: " + fullPath);
            Directory.CreateDirectory(fullPath);
            yield break;
        }

        string[] files = Directory.GetFiles(fullPath, "*.jpg");
        if (files.Length == 0)
            files = Directory.GetFiles(fullPath, "*.png");

        System.Array.Sort(files);

        foreach (string file in files)
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + file);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                pptSlides.Add(texture);

                if (pptSlides.Count == 1)
                    ShowSlide(0);
            }
            else
            {
                Debug.LogError("无法加载幻灯片: " + file + " - " + www.error);
            }
        }

        Debug.Log("已加载 " + pptSlides.Count + " 张PPT幻灯片。");
    }

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.PageDown))
        //     NextSlide();
        if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.PageUp))
            PreviousSlide();
    }

    public void NextSlide()
    {
        if (pptSlides.Count == 0) return;
        currentSlideIndex = (currentSlideIndex + 1) % pptSlides.Count;
        ShowSlide(currentSlideIndex);
    }

    public void PreviousSlide()
    {
        if (pptSlides.Count == 0) return;
        currentSlideIndex = (currentSlideIndex - 1 + pptSlides.Count) % pptSlides.Count;
        ShowSlide(currentSlideIndex);
    }

    private void ShowSlide(int index)
    {
        if (pptSlides.Count == 0 || index < 0 || index >= pptSlides.Count)
        {
            Debug.LogWarning("无效的幻灯片索引: " + index);
            return;
        }

        Texture2D slideTexture = pptSlides[index];
        instanceMaterial.SetTexture(texturePropertyName, slideTexture);
        
        if (preserveAspectRatio && resizeScreen)
        {
            // 调整屏幕/白板物体的尺寸以匹配PPT比例
            AdjustScreenSize(slideTexture);
        }
        else if (preserveAspectRatio)
        {
            // 仅调整纹理坐标，不改变物体尺寸
            AdjustTextureCoordinates(slideTexture);
        }
        else
        {
            // 不保持纵横比，填满整个目标
            instanceMaterial.mainTextureScale = Vector2.one;
            instanceMaterial.mainTextureOffset = Vector2.zero;
            
            // 恢复原始尺寸
            targetRenderer.transform.localScale = originalScale;
        }
        
        Debug.Log("显示幻灯片 " + (index + 1) + "/" + pptSlides.Count);
    }
    
    private void AdjustScreenSize(Texture2D slideTexture)
    {
        float slideAspect = (float)slideTexture.width / slideTexture.height;
        Vector3 newScale = originalScale;
        
        // 根据设置的参考维度决定如何调整尺寸
        switch (resizeReference)
        {
            case ResizeDimension.Width:
                // 保持宽度不变，调整高度
                newScale.y = originalScale.y * (originalScale.x / slideAspect) / originalScale.x;
                break;
                
            case ResizeDimension.Height:
                // 保持高度不变，调整宽度
                newScale.x = originalScale.x * (slideAspect * originalScale.y) / originalScale.y;
                break;
                
            case ResizeDimension.Auto:
            default:
                // 自动选择，保持整体面积基本不变
                if (slideAspect > 1) // 宽屏幻灯片
                {
                    newScale.x = originalScale.x;
                    newScale.y = originalScale.y / slideAspect;
                }
                else // 高屏幻灯片
                {
                    newScale.x = originalScale.x * slideAspect;
                    newScale.y = originalScale.y;
                }
                break;
        }
        
        // 应用比例调整系数
        newScale *= slideScale;
        
        // 应用新尺寸
        targetRenderer.transform.localScale = newScale;
        
        // 重置纹理坐标
        instanceMaterial.mainTextureScale = Vector2.one;
        instanceMaterial.mainTextureOffset = Vector2.zero;
    }
    
    private void AdjustTextureCoordinates(Texture2D slideTexture)
    {
        // 计算幻灯片的宽高比
        float slideAspect = (float)slideTexture.width / slideTexture.height;
        
        // 获取目标渲染器的尺寸比例
        Vector3 rendererSize = targetRenderer.bounds.size;
        float rendererAspect = rendererSize.x / rendererSize.y;
        
        // 调整纹理坐标
        Vector2 scale = Vector2.one;
        Vector2 offset = Vector2.zero;
        
        if (slideAspect > rendererAspect) 
        {
            // 幻灯片更宽，缩放高度
            scale.y = rendererAspect / slideAspect;
            offset.y = (1 - scale.y) * 0.5f;
        }
        else 
        {
            // 幻灯片更高，缩放宽度
            scale.x = slideAspect / rendererAspect;
            offset.x = (1 - scale.x) * 0.5f;
        }
        
        // 应用缩放系数
        scale *= slideScale;
        
        // 应用纹理坐标
        instanceMaterial.mainTextureScale = scale;
        instanceMaterial.mainTextureOffset = offset;
        
        // 恢复原始尺寸
        targetRenderer.transform.localScale = originalScale;
    }

    public void ShowSlideByIndex(int index)
    {
        if (index >= 0 && index < pptSlides.Count)
        {
            currentSlideIndex = index;
            ShowSlide(currentSlideIndex);
        }
    }

    public bool ImportPPTFromPath(string folderPath)
    {
        pptSlides.Clear();
        currentSlideIndex = 0;
        pptFolderPath = folderPath;
        StartCoroutine(LoadPPTSlides());
        return true;
    }
}