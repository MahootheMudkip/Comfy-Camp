using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using JetBrains.Annotations;

public class PlayerMovement : NetworkBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    public Animator anim;

    public float moveSpeed;
    private float moveInput;

    public float jumpForce;
    public Transform feetPos;
    public Vector2 jumpBoxDetectionSize;
    public LayerMask whatIsGround;

    private bool willJump;
    private bool willFall;

    private float coyoteTime = 0.05f;
    private float coyoteTimeCounter;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {   
        if (!IsOwner) return; 
        moveInput = Input.GetAxisRaw("Horizontal");
        anim.SetFloat("Speed", Mathf.Abs(moveInput));

        Jump();
    }

    void FixedUpdate()
    {
        // move left/right
        rb.velocity = new Vector2(moveInput * moveSpeed * Time.deltaTime, rb.velocity.y);
        FaceMoveDirection();

        // jump/fall 
        if (willJump) { rb.velocity = Vector2.up * jumpForce * Time.deltaTime; willJump = false;}
        if (willFall) { rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.6f); willFall = false;}
    }

    void Jump()
    {
        if (isGrounded())
        {
            coyoteTimeCounter = coyoteTime;
        } else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        if (Input.GetButtonDown("Jump") && coyoteTimeCounter > 0f)
        {
            willJump = true;
            return;
        }

        if (Input.GetButtonUp("Jump"))
        {
            willFall = true;
            coyoteTimeCounter = 0f;
            return;
        }
    }

    private bool isGrounded()
    {
        return Physics2D.OverlapBox(feetPos.position, jumpBoxDetectionSize, 0, whatIsGround);
    }

    // Flips player sprite when travelling in left/right direction
    private void FaceMoveDirection()
    {
        if (!IsLocalPlayer) return;

        if (moveInput > 0)
        {
            sr.flipX = false;
        }
        else if (moveInput < 0)
        {
            sr.flipX = true;
        }

        // notify server of the flip
        FlipSpriteServerRpc(sr.flipX);
    }

    // invoked by clients but executed on the server only
    [ServerRpc]
    void FlipSpriteServerRpc(bool state)
    {
        // make the change local on the server
        sr.flipX = state;

        // forward the change also to all clients
        FlipSpriteClientRpc(state);
    }

    // invoked by the server only but executed on ALL clients
    [ClientRpc]
    void FlipSpriteClientRpc(bool state)
    {
        // skip this function on the LocalPlayer 
        // because he is the one who originally invoked this
        if (IsLocalPlayer) return;

        //make the change local on all clients
        sr.flipX = state;
    }
}