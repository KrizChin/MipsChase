using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // External tunables.
    public float m_fMaxSpeed = 5.0f;
    public float m_fSlowSpeed = 3.0f;
    public float m_fIncSpeed = 2.0f;
    public float m_fMagnitudeFast = 0.15f;
    public float m_fMagnitudeSlow = 0.05f;
    public float m_fFastRotateSpeed = 0.2f;
    public float m_fFastRotateMax = 10.0f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;

    // Internal variables.
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fAngle;
    public float m_fSpeed;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public eState m_nState;
    public float m_fDiveStartTime;

    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(0,     0,   0),
        new Color(255, 255, 255),
        new Color(0,     0, 255),
        new Color(0,   255,   0),
    };

    public bool IsDiving()
    {
        return (m_nState == eState.kDiving);
    }

    void CheckForDive()
    {
        if (Input.GetMouseButton(0) && (m_nState != eState.kDiving && m_nState != eState.kRecovering))
        {
            // Start the dive operation
            m_nState = eState.kDiving;
            m_fSpeed = 0.0f;

            // Store starting parameters.
            m_vDiveStartPos = transform.position;
            m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
            m_fDiveStartTime = Time.time;
        }
    }

    void Start()
    {
        // Initialize variables.
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void UpdateDirectionAndSpeed()
    {
        // Get relative positions between the mouse and player
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(transform.position.x - vScreenPos.x, transform.position.y - vScreenPos.y);
        Debug.Log("Mouse world pos: " + vScreenPos);
        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        // Calculate how far away from the player the mouse is.
        float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;
        Debug.Log("MouseMagnitude: " + fMouseMagnitude +
                    "   Slow Thresh: " + m_fMagnitudeSlow +
                    "   Fast Thresh: " + m_fMagnitudeFast);
        // Based on distance, calculate the speed the player is requesting.
        if (fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (fMouseMagnitude > m_fMagnitudeSlow)
        {
            m_fTargetSpeed = m_fSlowSpeed;
        }
        else
        {
            m_fTargetSpeed = 0.0f;
        }
        Debug.Log("Target Speed set to: " + m_fTargetSpeed);
    }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    void Update()
    {
        // Update the direction from the mouse.
        UpdateDirectionAndSpeed();
        // Dive only when not diving or not recovering.
        CheckForDive();
        // Handle special state
        if (m_nState == eState.kDiving)
        {
            float t = (Time.time - m_fDiveStartTime) / Mathf.Max(0.0001f, m_fDiveTime);
            t = Mathf.Clamp01(t);
            transform.position = Vector3.Lerp(m_vDiveStartPos, m_vDiveEndPos, t);

            if (t >= 1.0f)
            {
                // Transition to recovery.
                m_nState = eState.kRecovering;
                m_fDiveStartTime = Time.time;
                m_fSpeed = 0.0f;
            }
            return;
        }

        if (m_nState == eState.kRecovering)
        {
            // Cannot move during recovery.
            m_fSpeed = 0.0f;

            if ((Time.time - m_fDiveStartTime) >= m_fDiveRecoveryTime)
            {
                m_nState = eState.kMoveSlow;
                m_fSpeed = 0.0f;
            }
            return;
        }

        // Make acceleration based on time instead of fps.
        float accel = m_fIncSpeed * Time.deltaTime;
        switch (m_nState)
        {
            case eState.kMoveSlow:
                {
                    // Slow: can turn immediately and speed ramps up
                    m_fAngle = m_fTargetAngle;
                    m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fTargetSpeed, accel);

                    // After building speed, enter fast state.
                    if (m_fSpeed >= m_fSlowSpeed && m_fTargetSpeed > 0.0f)
                    {
                        m_nState = eState.kMoveFast;
                    }
                    break;
                }

            case eState.kMoveFast:
                {
                    // Fast: limited turning or else you slow down.
                    float delta = Mathf.DeltaAngle(m_fAngle, m_fTargetAngle);
                    if (Mathf.Abs(delta) <= m_fFastRotateMax)
                    {
                        m_fAngle = Mathf.LerpAngle(m_fAngle, m_fTargetAngle, m_fFastRotateSpeed);
                        m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fTargetSpeed, accel);
                    }
                    else
                    {
                        // If mouse is outside turning range, slow down until it reaches slow state.
                        m_fSpeed = Mathf.MoveTowards(m_fSpeed, 0.0f, accel*3.0f);
                    }
                    if (m_fSpeed < m_fSlowSpeed)
                    {
                        m_nState = eState.kMoveSlow;
                    }
                    break;
                }
        }
        transform.rotation = Quaternion.Euler(0f, 0f, m_fAngle);
        transform.position += (-transform.right * m_fSpeed * Time.deltaTime);
        Debug.Log("Speed: " + m_fSpeed + "   Target: " + m_fTargetSpeed);
    }
}
