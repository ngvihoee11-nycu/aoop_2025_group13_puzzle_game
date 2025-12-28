using System.Collections.Generic;
using UnityEngine;

public class RigidbodyTraveller : PortalTraveller
{
    public Rigidbody rigid;

    public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        base.Teleport(fromPortal, toPortal, pos, rot);
        rigid.velocity = toPortal.TransformVector(Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)).MultiplyVector(fromPortal.InverseTransformVector(rigid.velocity)));
    }
}
