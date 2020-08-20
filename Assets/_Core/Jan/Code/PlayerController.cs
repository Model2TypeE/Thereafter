﻿using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SpatialTracking;

[DefaultExecutionOrder(-30005)]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private bool m_IsMouseControlled;
    [SerializeField] private bool m_IsDirectionallyControlled;
    [Header("Trackers and Cams")]
    [SerializeField] private Camera m_CamHMD;
    [SerializeField] private Transform m_EyeRight;
    [SerializeField] private Transform m_EyeLeft;

    [Header("Player Teleportation")]
    [SerializeField] private LayerMask m_CollidingLayers;
    [SerializeField] private Transform[] m_Controllers;
    [SerializeField] private Teleportable[] teleportables;
    [SerializeField] private LineRenderer m_LineRenderer;

    [Header("Movement")]
    [SerializeField] private float m_Movespeed = .05f;
    [SerializeField] private float m_MouseLookSpeed = 1f;

    public static float s_EyeSeperation = .05f;
    private Vector3 m_CurrentRotation = Vector3.zero;
    private Portal m_CurrentPortal;
    private XRInput m_Input;

    private void OnEnable()
    {
        Application.onBeforeRender += UpdatePortalProcedures;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= UpdatePortalProcedures;
    }

    private void Start()
    {
        m_LineRenderer.enabled = false;
        m_Input = XRInput.Instance;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        s_EyeSeperation = Vector3.Distance(m_EyeRight.position, m_EyeLeft.position);

        if (m_IsDirectionallyControlled)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                m_IsMouseControlled = !m_IsMouseControlled;
            }

            if (!m_IsMouseControlled)
            {
                m_CamHMD.GetComponent<TrackedPoseDriver>().enabled = true;
            }
            else
            {
                m_CamHMD.GetComponent<TrackedPoseDriver>().enabled = false;

                float vMouse = Input.GetAxis("Mouse X") * m_MouseLookSpeed;
                float hMouse = Input.GetAxis("Mouse Y") * -m_MouseLookSpeed;

                m_CurrentRotation = new Vector3(
                    Mathf.Clamp(m_CurrentRotation.x + hMouse, -90f, 90f),
                    m_CurrentRotation.y + vMouse,
                    0f);

                m_CamHMD.transform.rotation = Quaternion.Euler(m_CurrentRotation);
            }

            float hInput = Input.GetAxisRaw("Horizontal");
            float vInput = Input.GetAxisRaw("Vertical");
            var moveVec = m_CamHMD.transform.forward * vInput + m_CamHMD.transform.right * hInput;
            moveVec.y = 0f;
            transform.Translate(moveVec.normalized * m_Movespeed, Space.World);
        }
    }

    private bool m_IsTeleportGoalValid;
    private Vector3 m_TempTeleportGoalPos;
    [SerializeField] private Material m_LineRendererValidMat;
    [SerializeField] private Material m_LineRendererInvalidMat;
    private void FixedUpdate()
    {
        if(!m_IsDirectionallyControlled)
        {
            if (m_Input.GetTrigger(true))
            {
                m_IsTeleportGoalValid = CheckTeleportTargetPos(m_Controllers[0].position, m_Controllers[0].forward, out Vector3 targetPos);
                if(m_IsTeleportGoalValid)  m_TempTeleportGoalPos = targetPos;
            }
            else if (m_Input.GetTrigger(false))
            {
                m_IsTeleportGoalValid = CheckTeleportTargetPos(m_Controllers[1].position, m_Controllers[1].forward, out Vector3 targetPos);
                if (m_IsTeleportGoalValid) m_TempTeleportGoalPos = targetPos;
            }
            else
            {
                m_LineRenderer.enabled = false;
                if (m_IsTeleportGoalValid)
                {
                    m_IsTeleportGoalValid = false;
                    transform.position = m_TempTeleportGoalPos;
                }
            }
        }
    }

    private bool CheckTeleportTargetPos(Vector3 start, Vector3 dir, out Vector3 targetPos)
    {
        targetPos = Vector3.zero;
        if(Physics.Raycast(new Ray(start, dir), out RaycastHit hit, 10f, m_CollidingLayers))
        {
            m_LineRenderer.enabled = true;

            // check if target pos is valid on the nav mesh 
            // (if target pos can be stood on by player)
            bool isInNavMesh = NavMesh.SamplePosition(hit.point, out _, .1f, NavMesh.AllAreas);
            m_LineRenderer.SetPositions(new Vector3[] { start, hit.point });

            if (isInNavMesh)
            {
                targetPos = hit.point;
                m_LineRenderer.material = m_LineRendererValidMat;
                return true;
            }
            else
            {
                m_LineRenderer.material = m_LineRendererInvalidMat;
                return false;
            }
        }
        else
        {
            m_LineRenderer.enabled = false;
            return false;
        }
    }

    private void UpdatePortalProcedures()
    {
        if (m_CurrentPortal == null) return;

        var relVec = m_CamHMD.transform.position - m_CurrentPortal.transform.position;
        var angle = Vector3.Angle(m_CurrentPortal.transform.forward, relVec);
        if (angle < 90f)
        {
            m_CurrentPortal.Mirror(transform, transform);

            foreach(var teleportable in teleportables)
            {
                teleportable.m_cachedPortal = m_CurrentPortal.m_Pair;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        m_CurrentPortal = other.GetComponent<Portal>();
    }

    private void OnTriggerExit(Collider other)
    {
        m_CurrentPortal = null;
    }
}
