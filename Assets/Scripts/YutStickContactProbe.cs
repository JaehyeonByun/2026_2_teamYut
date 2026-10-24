using System.Collections.Generic;
using UnityEngine;

// Added automatically to each stick Rigidbody root. Do not attach to the Body child.
[DisallowMultipleComponent]
public sealed class YutStickContactProbe : MonoBehaviour
{
    private YutThrowPresenter owner;
    private int index;
    private readonly Dictionary<Collider,int> contacts=new Dictionary<Collider,int>();
    public void Initialize(YutThrowPresenter presenter,int stickIndex)
    { owner=presenter;index=stickIndex;Clear(); }
    public void Clear(){contacts.Clear();}
    public int ContactMask
    {
        get
        {
            int mask=0;
            foreach(var pair in contacts)
                if(pair.Key!=null && pair.Key.enabled && pair.Key.gameObject.activeInHierarchy) mask|=pair.Value;
            return mask;
        }
    }
    private void OnCollisionEnter(Collision collision){Refresh(collision);}
    private void OnCollisionStay(Collision collision){Refresh(collision);}
    private void OnCollisionExit(Collision collision){contacts.Remove(collision.collider);}
    private void OnDisable(){Clear();}
    private void Refresh(Collision collision)
    {
        if(owner==null || !owner.IsPlaying) { Clear(); return; }
        int mask=owner.GetSupportMask(index,collision);
        if(mask==0) contacts.Remove(collision.collider);
        else contacts[collision.collider]=mask;
    }
}
