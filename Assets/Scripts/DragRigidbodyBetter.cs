using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class DragRigidbodyBetter : MonoBehaviour
{

    [Tooltip("The spring force applied when dragging rigidbody. The dragging is implemented by attaching an invisible spring joint.")]
    public float Spring = 50.0f;
    public float Damper = 5.0f;
    public float Drag = 10.0f;
    public float AngularDrag = 5.0f;
    public float Distance = 0.2f;
    public float ScrollWheelSensitivity = 5.0f;
    public float RotateSpringSpeed = 10.0f;

    [Tooltip("Pin dragged spring to its current location.")]
    public KeyCode KeyToPinSpring = KeyCode.Space;

    [Tooltip("Delete all pinned springs.")]
    public KeyCode KeyToClearPins = KeyCode.Delete;

    [Tooltip("Twist spring.")]
    public KeyCode KeyToRotateLeft = KeyCode.Z;

    [Tooltip("Twist spring.")]
    public KeyCode KeyToRotateRight = KeyCode.C;

    [Tooltip("Set any LineRenderer prefab to render the used springs for the drag.")]
    public LineRenderer SpringRenderer;

    private int m_SpringCount = 1;
    private SpringJoint2D m_SpringJoint;
    private LineRenderer m_SpringRenderer;


    private void Update()
    {

        UpdatePinnedSprings();

        // Make sure the user pressed the mouse down
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        // We need to actually hit an object
        RaycastHit2D hit = (Physics2D.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition).origin,
                             Camera.main.ScreenPointToRay(Input.mousePosition).direction, 100,
                             Physics2D.DefaultRaycastLayers));
        // We need to hit a rigidbody that is not kinematic+

        if (!hit.rigidbody || hit.rigidbody.isKinematic)
        {
            return;
        }

        if (!m_SpringJoint)
        {
            var go = new GameObject("Rigidbody dragger-" + m_SpringCount);
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;
            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            m_SpringJoint = go.AddComponent<SpringJoint2D>();
            body.isKinematic = true;
            m_SpringCount++;

            if (SpringRenderer)
            {
                m_SpringRenderer = GameObject.Instantiate(SpringRenderer.gameObject, m_SpringJoint.transform, true).GetComponent<LineRenderer>();
            }
        }

        m_SpringJoint.transform.position = hit.point;
        m_SpringJoint.anchor = Vector3.zero;

        m_SpringJoint.autoConfigureDistance = false;
        m_SpringJoint.distance = Distance;
        m_SpringJoint.dampingRatio = Damper;
        m_SpringJoint.frequency = Spring;
        m_SpringJoint.connectedBody = hit.rigidbody;


        if (m_SpringRenderer)
        {
            m_SpringRenderer.enabled = true;
        }
        UpdatePinnedSprings();

        StartCoroutine(DragObject(hit.distance));
    }


    private IEnumerator DragObject(float distance)
    {
        var oldDrag = m_SpringJoint.connectedBody.drag;
        var oldAngularDrag = m_SpringJoint.connectedBody.angularDrag;
        m_SpringJoint.connectedBody.drag = Drag;
        m_SpringJoint.connectedBody.angularDrag = AngularDrag;
        var mainCamera = FindCamera();
        while (Input.GetMouseButton(0) && !Input.GetKeyDown(KeyToPinSpring))
        {
            distance += Input.GetAxis("Mouse ScrollWheel") * ScrollWheelSensitivity;

            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            m_SpringJoint.transform.position = ray.GetPoint(distance);

            var connectedPosition = m_SpringJoint.connectedBody.transform.position * m_SpringJoint.connectedAnchor;
            Vector3 vector3 = new Vector3(connectedPosition.x, connectedPosition.y, 0);
            var axis = m_SpringJoint.transform.position - vector3;

            yield return null;
        }


        if (m_SpringJoint.connectedBody)
        {
            m_SpringJoint.connectedBody.drag = oldDrag;
            m_SpringJoint.connectedBody.angularDrag = oldAngularDrag;

            if (Input.GetKeyDown(KeyToPinSpring))
            {
                m_SpringJoint = null;
                m_SpringRenderer = null;
            }
            else
            {
                m_SpringJoint.connectedBody = null;
                if (m_SpringRenderer)
                {
                    m_SpringRenderer.enabled = false;
                }
            }
        }
    }


    private void UpdatePinnedSprings()
    {
        foreach (Transform child in transform)
        {
            var spring = child.GetComponent<SpringJoint2D>();
            var renderer = child.GetComponentInChildren<LineRenderer>();

            if (!spring.connectedBody)
                continue;

            var connectedPosition = spring.connectedBody.transform.TransformPoint(spring.connectedAnchor);

            if (renderer && renderer.positionCount >= 2)
            {
                renderer.SetPosition(0, spring.transform.position);
                renderer.SetPosition(1, connectedPosition);
            }
        }

        if (Input.GetKeyDown(KeyToClearPins))
        {
            foreach (Transform child in transform)
            {
                if (m_SpringJoint == null || child.gameObject != m_SpringJoint.gameObject)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }
        }
    }

    private Camera FindCamera()
    {
        if (GetComponent<Camera>())
        {
            return GetComponent<Camera>();
        }

        return Camera.main;
    }
}
