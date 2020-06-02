﻿using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

[DefaultExecutionOrder(30000)]
public class PortalMain : PortalBase
{
    [SerializeField] private float m_EyeDist = .065f;
    [SerializeField] private RenderTexture m_RtEyeLeft;
    [SerializeField] private RenderTexture m_RtEyeRight;
    [Space]
    [SerializeField] private Camera m_EyeLeft;
    [SerializeField] private Camera m_EyeRight;
    private Camera m_MainCam;

    // testing

    //

    [Header("Cam Stuff - TODO")]
    protected int parameterHashVector;
    protected int parameterHashFloat;
    public Vector4 leftEye = new Vector4(0.0f, 0.0f, 1.0f, 0.5f);
    public Vector4 rightEye = new Vector4(0.0f, 0.5f, 1.0f, 0.5f);
    bool m_RightEyeQueued = false;

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += DoStuff;
        Application.onBeforeRender += OnBeforeRender;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= DoStuff;
        Application.onBeforeRender -= OnBeforeRender;
    }

    private void Start()
    {
        parameterHashVector = Shader.PropertyToID("_EyeTransformVector");
        parameterHashFloat = Shader.PropertyToID("_EyeFloatFlag");
        m_MainCam = Camera.main;
    }

    private void OnBeforeRender()
    {
        RenderLeft();
        RenderRight();

        //m_MainCam.Render();
    }

    private void RenderLeft(bool adjustPos = true)
    {
        if(adjustPos)
        {
            m_EyeLeft.projectionMatrix = m_MainCam.projectionMatrix;

            var relativePosition = transform.InverseTransformPoint(m_MainCam.transform.position);
            relativePosition = Vector3.Scale(relativePosition, new Vector3(-1, 1, -1));
            m_EyeLeft.transform.position = m_Pair.transform.TransformPoint(relativePosition);


            var relativeRotation = transform.InverseTransformDirection(m_MainCam.transform.forward);
            relativeRotation = Vector3.Scale(relativeRotation, new Vector3(-1, 1, -1));
            m_EyeLeft.transform.forward = m_Pair.transform.TransformDirection(relativeRotation);

            m_EyeLeft.transform.Rotate(new Vector3(0f, 0f, m_MainCam.transform.rotation.eulerAngles.z));
            m_EyeLeft.transform.Translate(new Vector3(-m_EyeDist * .5f, 0f, 0f), Space.Self);
        }

        SetActiveShaderEye(Camera.MonoOrStereoscopicEye.Left);
        m_EyeLeft.Render();
    }

    private void RenderRight(bool adjustPos = true)
    {
        if (adjustPos)
        {
            m_EyeRight.projectionMatrix = m_MainCam.GetStereoNonJitteredProjectionMatrix(Camera.StereoscopicEye.Right);

            var relativePosition = transform.InverseTransformPoint(m_MainCam.transform.position);
            relativePosition = Vector3.Scale(relativePosition, new Vector3(-1, 1, -1));
            m_EyeRight.transform.position = m_Pair.transform.TransformPoint(relativePosition);

            var relativeRotation = transform.InverseTransformDirection(m_MainCam.transform.forward);
            relativeRotation = Vector3.Scale(relativeRotation, new Vector3(-1, 1, -1));
            m_EyeRight.transform.forward = m_Pair.transform.TransformDirection(relativeRotation);

            m_EyeRight.transform.Rotate(new Vector3(0f, 0f, m_MainCam.transform.rotation.eulerAngles.z));
            m_EyeRight.transform.Translate(new Vector3(m_EyeDist * .5f, 0f, 0f), Space.Self);
        }

        SetActiveShaderEye(Camera.MonoOrStereoscopicEye.Right);
        m_EyeRight.Render();
    }

    private void DoStuff(ScriptableRenderContext ctx, Camera cam)
    {
        switch (cam.stereoTargetEye)
        {
            case StereoTargetEyeMask.Both:
                {
                    SetActiveShaderEye(Camera.MonoOrStereoscopicEye.Left);
                    m_RightEyeQueued = true;
                }
                break;

            case StereoTargetEyeMask.Left:
                {
                    SetActiveShaderEye(Camera.MonoOrStereoscopicEye.Left);
                }
                break;
            case StereoTargetEyeMask.Right:
                {
                    SetActiveShaderEye(Camera.MonoOrStereoscopicEye.Right);
                }
                break;

            case StereoTargetEyeMask.None:
                {
                    Debug.LogError("wtf");
                }
                break;
        }
    }

    private void OnRenderObject()
    {
        if (m_RightEyeQueued)
        {
            SetActiveShaderEye(Camera.MonoOrStereoscopicEye.Right);
            m_RightEyeQueued = false;
        }
    }

    private void SetActiveShaderEye(Camera.MonoOrStereoscopicEye eye)
    {
        Shader.SetGlobalVector(parameterHashVector, eye == Camera.MonoOrStereoscopicEye.Left ? leftEye : rightEye);
        Shader.SetGlobalFloat(
            parameterHashFloat, eye == Camera.MonoOrStereoscopicEye.Left ? 
            -1.0f : 
            (eye == Camera.MonoOrStereoscopicEye.Right ? 
                1.0f : 
                0.0f));
    }
}
