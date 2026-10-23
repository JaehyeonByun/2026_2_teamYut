using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// One mouse dispatcher owns piece hover, selection, and destination commits.
[DisallowMultipleComponent]
public class YutBoardPointer : MonoBehaviour
{
    [SerializeField] private LayerMask pickLayers = ~0;
    private YutTurnManager manager;
    private Camera viewCamera;
    private YutDestinationPicker picker;
    private YutPieceMovement[] pieces;
    private YutPieceOutline[] outlines;
    private YutPieceOutline[] opponentOutlines;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private int hover = -1;

    public bool Initialize(YutTurnManager owner, Camera camera, YutPieceMovement[] team, YutPieceMovement[] enemies, YutDestinationPicker destinations)
    {
        manager=owner;viewCamera=camera;pieces=team;picker=destinations;
        if(owner==null || camera==null || team==null || enemies==null || destinations==null) return false;
        outlines=new YutPieceOutline[team.Length];
        for(int i=0;i<team.Length;i++)
        {
            outlines[i]=team[i].GetComponent<YutPieceOutline>();
            if(outlines[i]==null) outlines[i]=team[i].gameObject.AddComponent<YutPieceOutline>();
            if(!outlines[i].isActiveAndEnabled || !outlines[i].Initialize()) return false;
        }
        opponentOutlines=new YutPieceOutline[enemies.Length];
        for(int i=0;i<enemies.Length;i++)
        {
            opponentOutlines[i]=enemies[i].GetComponent<YutPieceOutline>();
            if(opponentOutlines[i]==null) opponentOutlines[i]=enemies[i].gameObject.AddComponent<YutPieceOutline>();
            if(!opponentOutlines[i].isActiveAndEnabled || !opponentOutlines[i].Initialize()) return false;
        }
        return true;
    }
    private static bool ReadMouse(out Vector2 position,out bool pressed)
    {
#if ENABLE_INPUT_SYSTEM
        var mouse=Mouse.current;
        position=mouse==null?Vector2.zero:mouse.position.ReadValue();
        pressed=mouse!=null && mouse.leftButton.wasPressedThisFrame;
        return mouse!=null;
#elif ENABLE_LEGACY_INPUT_MANAGER
        position=Input.mousePosition;pressed=Input.GetMouseButtonDown(0);return Input.mousePresent;
#else
        position=Vector2.zero;pressed=false;return false;
#endif
    }
    private bool OverUI(Vector2 position)
    {
        var system=EventSystem.current;
        if(system==null) return false;
        var data=new PointerEventData(system){position=position};
        uiHits.Clear();system.RaycastAll(data,uiHits);
        // Ignore PhysicsRaycaster results: they represent the board, not blocking HUD.
        foreach(var hit in uiHits) if(hit.module is GraphicRaycaster) return true;
        return false;
    }
    private void LateUpdate()
    {
        hover=-1;
        if(manager!=null) manager.PreviewMove(null);
        if(manager==null || !manager.BoardPointerEnabled || viewCamera==null || !Application.isFocused)
        { RefreshVisuals();return; }
        Vector2 position;bool pressed;
        if(!ReadMouse(out position,out pressed) || !viewCamera.pixelRect.Contains(position) || OverUI(position))
        { RefreshVisuals();return; }
        Physics.SyncTransforms(); // Pieces move by Transform, not Rigidbody simulation.
        Ray ray=viewCamera.ScreenPointToRay(position);
        RaycastHit[] hits=Physics.RaycastAll(ray,viewCamera.farClipPlane,pickLayers,QueryTriggerInteraction.Collide);
        Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        YutPieceMovement hitPiece=null;
        float obstruction=float.PositiveInfinity;
        foreach(var hit in hits)
        {
            var piece=hit.collider.GetComponentInParent<YutPieceMovement>();
            if(piece!=null){hitPiece=piece;obstruction=hit.distance;break;}
            if(!hit.collider.isTrigger){obstruction=hit.distance;break;}
            // Existing node trigger spheres must not block the mouse.
        }
        int index=hitPiece==null?-1:Array.IndexOf(pieces,hitPiece);
        bool selectedGroup=index>=0 && manager.IsSelectedPlayerGroup(index);
        YutDestinationPicker.Hit destination = new YutDestinationPicker.Hit();
        bool bodyTarget=hitPiece!=null && !selectedGroup && picker.TryGetNodeHit(hitPiece.CurrentNodeId,out destination);
        bool destinationHit=bodyTarget;
        if(!bodyTarget)
        {
            destinationHit=picker.TryHit(ray,out destination);
            if(destinationHit && obstruction+0.001f<destination.Distance)
                destinationHit=hitPiece!=null && hitPiece.CurrentNodeId==destination.Node;
        }
        if(YutSelectionRules.PreferDestination(destinationHit,selectedGroup))
        {
            manager.PreviewMove(destination.Move);
            if(pressed) picker.Choose(destination);
        }
        else if(index>=0 && manager.CanPointerSelectPiece(index))
        {
            hover=index;
            if(pressed) manager.SelectPlayerPiece(index);
        }
        RefreshVisuals();
    }
    public void RefreshVisuals()
    {
        if(outlines==null) return;
        bool active=manager!=null && manager.BoardPointerEnabled && isActiveAndEnabled;
        for(int i=0;i<outlines.Length;i++)
        {
            if(outlines[i]==null) continue;
            bool selected=active && (manager.IsSelectedPlayerGroup(i) || manager.IsStackablePlayer(i));
            bool hovered=active && hover>=0 && manager.SamePlayerGroup(i,hover) && manager.CanPointerSelectPiece(i);
            outlines[i].SetState(hovered,selected,picker==null?1f:picker.HighlightAlpha);
        }
        if(opponentOutlines!=null)
            for(int i=0;i<opponentOutlines.Length;i++)
                if(opponentOutlines[i]!=null) opponentOutlines[i].SetState(false,active && manager.IsCapturableOpponent(i),picker==null?1f:picker.HighlightAlpha);
    }
    private void OnDisable()
    {
        hover=-1;
        if(outlines!=null) foreach(var outline in outlines) if(outline!=null) outline.SetState(false,false);
        if(opponentOutlines!=null) foreach(var outline in opponentOutlines) if(outline!=null) outline.SetState(false,false);
    }
}
