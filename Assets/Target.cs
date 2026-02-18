using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
   {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
   };

    // External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 3.0f;
    public float m_fScaredDistance = 3.0f;
    public int m_nMaxMoveAttempts = 5;

    // Internal variables.
    public eState m_nState;
    public float m_fHopStart;
    public Vector3 m_vHopStartPos;
    public Vector3 m_vHopEndPos;

    //Added variables
    public float m_fScreenPad = 0.4f;
    // Initialize Taunt 
    public float m_fTauntRadius = 2.5f;
    public float m_fTauntCooldown = 0.5f;
    public AudioSource m_tauntAudio;
    public AudioClip m_laughClip;
    private bool m_wasPlayerDiving = false;
    private bool m_caughtDuringThisDive = false;
    private float m_lastTauntTime = -999f;

    void Start()
    {
        // Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindObjectOfType(typeof(Player)) as Player;
    }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving())
            {
                m_caughtDuringThisDive = true;
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            }
        }
    }

    Vector2 WorldMin()
    {
        Vector3 v = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        return new Vector2(v.x, v.y);
    }
    Vector2 WorldMax()
    {
        Vector3 v = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
        return new Vector2(v.x, v.y);
    }

    Vector3 ClampToScreen(Vector3 p)
    {
        Vector2 mn = WorldMin();
        Vector2 mx = WorldMax();
        p.x = Mathf.Clamp(p.x, mn.x + m_fScreenPad, mx.x - m_fScreenPad);
        p.y = Mathf.Clamp(p.y, mn.y + m_fScreenPad, mx.y - m_fScreenPad);
        return p;
    }

    void ChooseHopDestination()
    {
        m_vHopStartPos = transform.position;

        Vector2 away = (m_vHopStartPos - m_player.transform.position);
        if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle;
        away.Normalize();

        Vector3 best = m_vHopStartPos;
        float bestScore = -9f;

        for (int i = 0; i < m_nMaxMoveAttempts; i++)
        {
            Vector2 dir = (away + Random.insideUnitCircle * 0.6f).normalized;

            Vector3 candidate = m_vHopStartPos + (Vector3)(dir * m_fHopSpeed);
            candidate = ClampToScreen(candidate);

            float score = Vector3.Distance(candidate, m_player.transform.position);

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        m_vHopEndPos = best;
    }

    void Update()
    {
        //Debug.Log("Target Update Running");
        if (m_player == null) 
        {
            //Debug.Log("No player found by target");
            return;
        }
        // Taunt Mechanic
        bool divingNow = m_player.IsDiving();

        if (!m_wasPlayerDiving && divingNow)
        {
            m_caughtDuringThisDive = false;
        }
        if (m_wasPlayerDiving && !divingNow && m_nState != eState.kCaught)
        {
            float d = Vector3.Distance(transform.position, m_player.transform.position);
            bool near = d <= m_fTauntRadius;

            if (near && !m_caughtDuringThisDive && (Time.time - m_lastTauntTime) >= m_fTauntCooldown)
            {
                if (m_tauntAudio != null && m_laughClip != null)
                {
                    m_tauntAudio.PlayOneShot(m_laughClip);
                }
                m_lastTauntTime = Time.time;
            }
        }
        m_wasPlayerDiving = divingNow;

        // Rabbit State Machine
        switch (m_nState)
        {
            case eState.kIdle:
            {
                // Stay in one place until player gets close
                float d = Vector3.Distance(transform.position, m_player.transform.position);
                Debug.Log("Distance to player: " + d);
                if (d <= m_fScaredDistance)
                    m_nState = eState.kHopStart;
                break;
            }

            case eState.kHopStart:
            {
                ChooseHopDestination();
                m_fHopStart = Time.time;
                m_nState = eState.kHop;
                break;
            }

            case eState.kHop:
            {
                // Quick but visible hop
                float t = (Time.time - m_fHopStart) / Mathf.Max(0.0001f, m_fHopTime);
                t = Mathf.Clamp01(t);

                transform.position = Vector3.Lerp(m_vHopStartPos, m_vHopEndPos, t);

                if (t >= 1.0f)
                    m_nState = eState.kIdle;

                break;
            }

            case eState.kCaught:
            {
                break;
            }
        }
    }
}