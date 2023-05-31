using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class WallScaler : MonoBehaviour
{
    public Transform HandleStart;
    public Transform HandleEnd;
    public Transform HandleHeight;
    public Transform ObjectToScale;

#if UNITY_EDITOR
    private Vector3 _initialScale;
    private Vector3 _initialHandleStartLocalPosition;
    private Vector3 _initialHandleEndLocalPosition;
    private Vector3 _initialHandleHeightLocalPosition;

    private void OnEnable()
    {
        // Store the initial scale and handle local positions
        _initialScale = ObjectToScale.localScale;
        _initialHandleStartLocalPosition = new Vector3(_initialScale.x*0.5f, 0.5f, 0f);
        _initialHandleEndLocalPosition = new Vector3(-_initialScale.x*0.5f, 0.5f, 0f);
        _initialHandleHeightLocalPosition = new Vector3(0, _initialScale.y, 0f);
        HandleStart.localPosition = _initialHandleStartLocalPosition;
        HandleEnd.localPosition = _initialHandleEndLocalPosition;
        HandleHeight.localPosition = _initialHandleHeightLocalPosition;
        // Ensure the object is properly scaled at the start of level design
        ApplyScale();
    }

    private void Update()
    {
        if (!IsSelected())
        {
            ShowHandles(false);
            return;
        }
        ShowHandles(true);
        if (!EditorApplication.isPlaying)
        {
            // Get the current handle local positions
            Vector3 HandleStartPosition = HandleStart.position;
            Vector3 HandleEndPosition = HandleEnd.position;
            Vector3 handleStartLocalPosition = transform.InverseTransformPoint(HandleStartPosition);
            Vector3 handleEndLocalPosition = transform.InverseTransformPoint(HandleEndPosition);
            Vector3 handleHeightLocalPosition = transform.InverseTransformPoint(HandleHeight.position);


            // Constrain the handle movement along the local X-axis
            handleStartLocalPosition.y = _initialHandleStartLocalPosition.y;
            handleStartLocalPosition.z = _initialHandleStartLocalPosition.z;
            handleEndLocalPosition.y = _initialHandleEndLocalPosition.y;
            handleEndLocalPosition.z = _initialHandleEndLocalPosition.z;

            // Calculate the new scale based on the constrained handle local positions
            Vector3 scale = new Vector3(
                Vector3.Distance(handleStartLocalPosition, handleEndLocalPosition),
                handleHeightLocalPosition.y,
                _initialScale.z
            );

            // Apply the new scale to the object to scale
            ObjectToScale.localScale = scale;
            
            // Adjust the position of the box shape based on the new scale
            Vector3 handlesCenter = HandleStartPosition*0.5f + HandleEndPosition*0.5f;
            transform.position = new Vector3(handlesCenter.x, transform.position.y, handlesCenter.z);
            
            Vector3 objectToScaleLocalPosition =  ObjectToScale.localPosition;
            objectToScaleLocalPosition.y = scale.y / 2;
            ObjectToScale.localPosition = objectToScaleLocalPosition;
            

            // Update the handle local positions
            HandleStart.localPosition = new Vector3(scale.x*0.5f, _initialHandleStartLocalPosition.y, 0f);
            HandleEnd.localPosition = new Vector3(-scale.x*0.5f, _initialHandleEndLocalPosition.y, 0f);

            // Constrain the height handle movement along the local X and Z-axes
            handleHeightLocalPosition.x = _initialHandleHeightLocalPosition.x;
            handleHeightLocalPosition.z = _initialHandleHeightLocalPosition.z;

            // Update the height handle position
            HandleHeight.localPosition = new Vector3(objectToScaleLocalPosition.x, HandleHeight.localPosition.y, objectToScaleLocalPosition.z);
        }
    }

    private bool IsSelected()
    {
        GameObject currentlySelected = Selection.activeGameObject;
        if (currentlySelected == HandleEnd.gameObject || currentlySelected == HandleStart.gameObject ||
            currentlySelected == HandleHeight.gameObject || currentlySelected == gameObject || 
            currentlySelected == ObjectToScale.gameObject)
        {
            return true;
        }

        return false;
    }
    [Button("Set Width")]
    public void SetWidth(float width)
    {
        Vector3 newScale = new Vector3(width, ObjectToScale.localScale.y, ObjectToScale.localScale.z);
        ObjectToScale.localScale = newScale;
        PositionHandles(newScale);
    }

    private void PositionHandles(Vector3 scale)
    {
        
        // Update the handle local positions
        HandleStart.localPosition = new Vector3(scale.x*0.5f, _initialHandleStartLocalPosition.y, 0f);
        HandleEnd.localPosition = new Vector3(-scale.x*0.5f, _initialHandleEndLocalPosition.y, 0f);

        // Update the height handle position
        HandleHeight.localPosition = new Vector3(0, HandleHeight.localPosition.y, 0);
    }
    
#endif

#if UNITY_EDITOR
    private void ApplyScale()
    {
        // Get the current handle local positions
        Vector3 handleStartLocalPosition = transform.InverseTransformPoint(HandleStart.position);
        Vector3 handleEndLocalPosition = transform.InverseTransformPoint(HandleEnd.position);

        // Constrain the handle movement along the local X-axis
        handleStartLocalPosition.y = _initialHandleStartLocalPosition.y;
        handleStartLocalPosition.z = _initialHandleStartLocalPosition.z;
        handleEndLocalPosition.y = _initialHandleEndLocalPosition.y;
        handleEndLocalPosition.z = _initialHandleEndLocalPosition.z;

        // Calculate the new scale based on the constrained handle local positions
        Vector3 scale = new Vector3(
            Vector3.Distance(handleStartLocalPosition, handleEndLocalPosition),
            _initialScale.y,
            _initialScale.z
        );

        // Apply the new scale to the object to scale
        ObjectToScale.localScale = scale;

        // Adjust the position of the box shape based on the new scale
        ObjectToScale.position = (HandleStart.position + HandleEnd.position) / 2f;

        // Update the handle local positions
        HandleStart.position = transform.TransformPoint(handleStartLocalPosition);
        HandleEnd.position = transform.TransformPoint(handleEndLocalPosition);
    }
#endif
    
    private void ShowHandles(bool show)
    {
#if UNITY_EDITOR
        HandleStart.gameObject.SetActive(show);
        HandleEnd.gameObject.SetActive(show);
        HandleHeight.gameObject.SetActive(show);
#endif
    }
}
