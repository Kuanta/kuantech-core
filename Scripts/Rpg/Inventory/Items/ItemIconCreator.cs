// using System.Collections.Generic;
// using System.IO;
// using Cysharp.Threading.Tasks;
// using Kuantech.Core;
// using Kuantech.Data;
// using UnityEngine;
// using UnityEngine.UI;
// using Sirenix.OdinInspector;
//
// namespace Kuantech.Rpg.Inventory
// {
//     public class ItemIconCreator : MonoBehaviour
//     {
//         [SerializeField] private Camera Camera;
//         [SerializeField] private int Width;
//         [SerializeField] private int Height;
//         //[SerializeField] private RenderTexture _renderTexture;
//         [SerializeField] private Image Image;
//
//         [Header("Photoshoot positions")] 
//         [SerializeField] private CharacterBody Mannequin;
//         [SerializeField] private Transform HeadPosition;
//         [SerializeField] private Transform ChestPosition;
//         [SerializeField] private Transform LegsPosition;
//         [SerializeField] private Transform WeaponPosition;
//         [SerializeField] private Transform BowPosition;
//         
//         //todo: We don't need 2 dicts but prefab ıds for in-place and other items aren't unique
//         private Dictionary<int, Sprite> _itemSprites = new Dictionary<int, Sprite>();
//         private Dictionary<int, Sprite> _inplaceItemSprite = new Dictionary<int, Sprite>();
//         
//         private RenderTexture _renderTexture;
//
//         
//         private void Awake()
//         {
//             if (!Application.isPlaying) return;
//             if (_renderTexture == null) _renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
//             Camera.targetTexture = _renderTexture;
//             Mannequin.Initialize();
//         }
//         
//         [Button("Take A Shot")]
//         public async void SetIconForImage(string filename = "shot.png")
//         {
//             if (!Application.isPlaying)
//             {
//                 _renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
//
//             }
//             Sprite sprite = await SnapshotAndSave(filename);
//             if(Image != null) Image.sprite = sprite;
//             if (!Application.isPlaying)
//             {
//                 Camera.targetTexture = null;
//                 DestroyImmediate(_renderTexture);
//             }
//         }
//
//         private async UniTask<Sprite> SnapshotAndSave(string filename = "shot.png")
//         {
//             Sprite sprite = CreateSprite(512, 512);
//             await Snapshot(sprite.texture);
//             SaveSprite(sprite, "Kuantech/Art/Sprites/Items", filename);
//             _renderTexture.Release();
//             return sprite;
//         }
//
//         public void SaveSprite(Sprite sprite, string filePath,string filename)
//         {
//             byte[] pngBytes = sprite.texture.EncodeToPNG();
//             string source = Path.Combine(Application.dataPath, filePath);
//             File.WriteAllBytes(Path.Combine(source, filename), pngBytes);
//         }
//         
//         [Button("Photoshoot")]
//         public async void SaveArmorIcons()
//         {
//             CharacterBody characterBody = Mannequin;
//             foreach (var equipment in characterBody.InplaceEquipmentsList)
//             {
//                 int id = equipment.TemplateId;
//                 PositionMannequin(characterBody, id);
//                 string fileName = "equipment_" + id + ".png";
//                 SnapshotAndSave(fileName);
//                 characterBody.ToggleInplaceEditor(id, false);
//             }
//         }
//         
//         /// <summary>
//         /// For dynamiccaly icon generation
//         /// </summary>
//         /// <param name="characterBody"></param>
//         /// <param name="inplaceEquipmentId"></param>
//         public void PositionMannequin(CharacterBody characterBody, int inplaceEquipmentId)
//         {
//             // InplaceEquipment equipment = characterBody.InplaceEquipments[inplaceEquipmentId];
//             // int id = equipment.TemplateId;
//             // switch (equipment.EquipmentType)
//             // {
//             //     case Enums.EquipmentSlotType.Head:
//             //         characterBody.transform.SetParent(HeadPosition);
//             //         break;
//             //     case Enums.EquipmentSlotType.Chest:
//             //         characterBody.transform.SetParent(ChestPosition);
//             //         break;
//             //     case Enums.EquipmentSlotType.Legs:
//             //         characterBody.transform.SetParent(LegsPosition);
//             //         break;
//             // }
//             //     
//             // characterBody.SlotInplaceEquipment(id);
//             // characterBody.transform.localPosition = Vector3.zero;
//             // characterBody.transform.localRotation = Quaternion.identity;
//         }
//         
//         /// <summary>
//         /// Returns a texture from the camera's eye
//         /// </summary>
//         /// <returns></returns>
//         private async UniTask Snapshot(Texture2D texture)
//         {
//            // Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
//             Rect rect = new Rect(0,0,Width, Height);
//             Camera.targetTexture = _renderTexture;
//             _renderTexture.Release();
//             Camera.Render();
//             RenderTexture currentRenderTexture = RenderTexture.active;
//             RenderTexture.active = _renderTexture;
//             texture.ReadPixels(rect, 0, 0);
//             texture.Apply();
//             
//             //Re-apply default frame buffer
//             RenderTexture.active = currentRenderTexture;
//             await UniTask.DelayFrame(1);
//         }
//         
//         /// <summary>
//         /// Returns the sprite for an item.
//         /// Since it activates and deactivates some game objects, a single frame must be waited for correct rendering
//         /// </summary>
//         /// <param name="templateId"></param>
//         /// <returns></returns>
//         public Sprite GetItemIcon(int itemId)
//         {
//             ItemData itemData = Librarian.Instance.ItemDatas[itemId];
//             //int prefabId = itemData.Template.prefabId;
//             Sprite sprite = null;
//             return sprite; //todo fixx!
//             // //Is in-place?
//             // if (itemData.Template.inPlace)
//             // {
//             //     if (_inplaceItemSprite.ContainsKey(prefabId))
//             //         return _inplaceItemSprite[prefabId];
//             //     
//             //     //Create new sprite and add it to dict
//             //     sprite = CreateSprite(512, 512);
//             //     _inplaceItemSprite[prefabId] = sprite;
//             //
//             //     Mannequin.gameObject.SetActive(true);
//             //     PositionMannequin(Mannequin, prefabId);
//             //     Snapshot(sprite.texture);
//             //     Mannequin.RemoveInplaceObject(prefabId);
//             //     //Debug.Log("Removing item "+prefabId);
//             //     Mannequin.gameObject.SetActive(false);
//             //     return sprite;
//             // }
//             // else
//             // {
//             //     if (_itemSprites.ContainsKey(prefabId)) return _itemSprites[prefabId]; //Check for existing
//             //     sprite = CreateSprite(512, 512);
//             //     _itemSprites[prefabId] = sprite;
//             //
//             //     Mannequin.gameObject.SetActive(false);
//             //     //Place weapon
//             //     GameObject modelPrefab = Librarian.Instance.GetItemTemplatePrefab(itemId).ItemPrefab;
//             //     GameObject model = GameManager.Instance.Pool.GetObject(modelPrefab);
//             //     if (Librarian.Instance.ItemDatas[itemId] is WeaponData {WeaponType: Enums.WeaponType.Bow})
//             //     {
//             //         model.transform.SetParent(BowPosition);
//             //     }
//             //     else
//             //     {
//             //         model.transform.SetParent(WeaponPosition);
//             //     }
//             //     model.transform.localPosition = Vector3.zero;
//             //     model.transform.localRotation = Quaternion.identity;
//             //     model.transform.localScale = Vector3.one;
//             //     Snapshot(sprite.texture);
//             //     model.SetActive(false);
//             //     Destroy(model);
//             //     return sprite;
//             // }
//         }
//
//         private Sprite CreateSprite(int width, int height)
//         {
//             return Sprite.Create(new Texture2D(Width, Height, TextureFormat.RGBA32, false),  new Rect(0,0,Width, Height), Vector2.zero);
//         }
//     }
// }