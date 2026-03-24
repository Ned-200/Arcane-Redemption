using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class OpenSign : MonoBehaviour
{
    private bool playerInRange = false;
    protected PlayerController playerController;

    [Header("UI")]
    [SerializeField] private GameObject interactPromptPrefab;
    private GameObject signView;
    private Image signUI;
    private GameObject interactPrompt;
    private bool viewingSign;
    [SerializeField] private DecalProjector decalProjector;


    void Start()
    {
        // Get Sign UI
        GameObject canvas = GameObject.FindWithTag("MainCanvas");
        signView = canvas.transform.Find("SignView").gameObject;
        if (signView != null)
        {
            signUI = signView.transform.Find("SignImage").GetComponent<Image>();
            if (signUI == null)
            {
                Debug.LogError("OpenSign: Could not find signUI! Check naming and children!");
            }
        } else {
            Debug.LogError("OpenSign: Could not find signView! Check naming and children!");
        }
        
        // Get interactPrompt prefab
        if (interactPromptPrefab == null)
        {
            Debug.LogError("OpenSign: interactPromptPrefab not assigned! Please assign the prefab.");
        }

        // Get decalProjector
        if (decalProjector == null)
        {
            Debug.LogError("OpenSign: decalProjector not assigned! Please assign the decal.");
        }
    }

    void Update()
    {
        if (!viewingSign && playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            // Set UI image to sign decal
            signUI.sprite = MaterialToSprite(decalProjector.material);
            signUI.SetNativeSize();

            // Show sign in UI
            signView.SetActive(true);
            viewingSign = true;

            // disable player movement
            playerController.canMove = false;
        }

        if (viewingSign && Input.GetMouseButtonDown(0))
        {
            // Hide sign in UI
            signView.SetActive(false);
            viewingSign = false;

            // enable player movement
            playerController.canMove = true;
        }
    }

    private Sprite MaterialToSprite(Material material) {
        // Texture tex = material.GetTexture("_BaseMap");
        Texture tex = material.mainTexture;
        if (tex == null)
        {
            Debug.LogError("What the hell");
        }

        Texture2D tex2D = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);

        // Do some BS
        RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height);
        Graphics.Blit(tex, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex2D.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);


        Sprite blankSprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0.5f, 0.5f));

        if (blankSprite == null)
        {
            Debug.LogError("What the fuck");
        }

        return blankSprite;
    }


    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && !viewingSign)
        {   
            // Get playerController
            playerController = other.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("OpenSign: playerController NOT FOUND BY Sign! Check Player Hierarchy.");
            }

            playerInRange = true;
            Debug.Log("Entered sign range");
            if (interactPromptPrefab != null)
            {
                interactPrompt = Instantiate(interactPromptPrefab, new Vector3(this.transform.position.x, this.transform.position.y+3.0f, this.transform.position.z), this.transform.rotation);
            } else {
                Debug.LogError("OpenSign: Interact Prompt prefab not assigned! " + this.gameObject.name);
            }
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && !viewingSign)
        {
            playerInRange = false;
            Debug.Log("Left sign range");
            Destroy(interactPrompt);
        }
    }
}
