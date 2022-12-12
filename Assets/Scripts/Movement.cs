using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public bool canMove = true;
    [SerializeField] protected float movementSpeed = 5;
    protected Vector2 moveVelocity;

    private void FixedUpdate()
    {
        if (canMove)
        {
            Move(moveVelocity);
        }
    }

    protected virtual void Move(Vector2 velocity)
    {

    }
}
