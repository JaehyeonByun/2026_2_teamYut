using UnityEngine;
using UnityEngine.Rendering;

namespace SoulFlameAsset
{
    // Closed 3D flame meshes. Keeps health presenter and legacy prefab API.
    public sealed class SoulFlameVisual : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private ParticleSystem core, sparks, hitBurst;
        [SerializeField] private Light glow;
        [SerializeField] private Color flameColor = new Color(.1f,.85f,1f,1f);
        [SerializeField, Min(.05f)] private float fadeSeconds = .45f;
        [SerializeField, Min(0f)] private float lightIntensity = .7f;
        [Header("Toon 3D flame")]
        [SerializeField, Min(.05f)] private float worldHeight = .32f;
        [SerializeField, Range(.25f,1.5f)] private float widthRatio = .85f;
        [SerializeField, Min(0f)] private float baseOffset = .025f;
        [SerializeField, Min(.1f)] private float hitSeconds = .6f;
        [SerializeField, Range(1,6)] private int blinkCount = 3;
        [SerializeField] private bool warmPalette;
        private GameObject art;
        private Mesh mesh;
        private Renderer[] toonRenderers;
        [SerializeField, Range(0f,.15f)] private float swayAmount=.045f;
        [SerializeField, Range(.1f,5f)] private float swaySpeed=1.6f;
        private Material material;
        private Camera windViewCamera;
        private float strength=1f,target=1f,hitRemaining;
        private int lastHP=-1,lastMax=-1;
        private bool initialized;
        private bool healthHoverEnabled=true;
        [SerializeField, Min(.1f)] private float windHitMinimumSeconds=.85f;
        [SerializeField, Range(0f,.65f)] private float hitWindBend=.38f;
        public int CurrentHP => Mathf.Max(0,lastHP);
        public int MaximumHP => Mathf.Max(1,lastMax);
        public bool HasHealth => initialized && lastHP>=0;
        public bool IsOpponentFlame => warmPalette;
        public Bounds HealthHoverBounds
        {
            get
            {
                float height=Mathf.Max(.10f,strength*worldHeight);
                return new Bounds(transform.position+Vector3.up*(baseOffset+height*.5f),
                    new Vector3(Mathf.Max(.12f,height*1.5f),height*1.2f,Mathf.Max(.12f,height*1.5f)));
            }
        }
        public Vector3 HealthLabelPoint => transform.position+Vector3.up*(baseOffset+Mathf.Max(.10f,strength*worldHeight)+.045f);
        public float HitSeconds => Mathf.Max(windHitMinimumSeconds,hitSeconds);
        public float FadeSeconds => fadeSeconds;
        public bool IsExtinguished => lastHP==0 && hitRemaining<=0f && strength<=.001f;
        public void Configure(Transform root,ParticleSystem mainFire,ParticleSystem embers,
            ParticleSystem impact,Light source,Color color)
        { visualRoot=root;core=mainFire;sparks=embers;hitBurst=impact;glow=source;flameColor=color;warmPalette=color.r>color.b; }
        public void SetTeam(bool opponent) { warmPalette=opponent; }
        // Call on a NEW candle host before initialization. Does not alter scale flames.
        public void ConfigureIndicator(float height,float offset,float fade)
        {
            healthHoverEnabled=false;
            worldHeight=Mathf.Max(.01f,height);baseOffset=Mathf.Max(0f,offset);
            fadeSeconds=Mathf.Max(.05f,fade);
        }
        public void SetIndicatorLit(bool lit,bool instant=false)
        {
            if(!Initialize())return;
            lastHP=lastMax=-1;hitRemaining=0f;
            target=lit?1f:0f;
            if(instant)strength=target;
        }
        private void Start() { Initialize(); }
        private bool Initialize()
        {
            if(initialized) return true;
            var shader=Resources.Load<Shader>("YutToonFlame/ToonFlame");
            if(shader==null || !shader.isSupported)
            { Debug.LogError("SoulFlameVisual: import Resources/YutToonFlame/ToonFlame.shader (Built-in/URP 3D).",this); enabled=false; return false; }
            StopLegacy(core);StopLegacy(sparks);StopLegacy(hitBurst);
            if(glow!=null) glow.enabled=false;
            // A world-space root avoids inherited pan tilt and scale distortion.
            art=new GameObject("ToonFlame3D_Runtime");art.layer=gameObject.layer;
            material=new Material(shader){name="ToonSoulFlame_Runtime"};
            mesh=BuildFlameMesh();
            var renderers=new System.Collections.Generic.List<Renderer>();
            MakeTongue("MainFlame",Vector3.zero,new Vector3(1,1,1),0f,renderers);
            MakeTongue("LeftTongue",new Vector3(-.19f,.015f,.015f),new Vector3(.58f,.68f,.67f),-26f,renderers);
            MakeTongue("RightTongue",new Vector3(.19f,.025f,-.015f),new Vector3(.48f,.51f,.55f),32f,renderers);
            toonRenderers=renderers.ToArray();
            initialized=true;
            if(healthHoverEnabled && GetComponent<YutFlameHealthTooltip>()==null) gameObject.AddComponent<YutFlameHealthTooltip>();
            return true;
        }
        private void MakeTongue(string name,Vector3 position,Vector3 size,float angle,System.Collections.Generic.List<Renderer> renderers)
        {
            var obj=new GameObject(name);obj.layer=gameObject.layer;
            obj.transform.SetParent(art.transform,false);obj.transform.localPosition=position;
            obj.transform.localScale=size;obj.transform.localRotation=Quaternion.Euler(0,0,angle);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=obj.AddComponent<MeshRenderer>();r.sharedMaterial=material;
            r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;renderers.Add(r);
        }
        // Closed surface of revolution, bent towards its pointed tip. No quads/billboards.
        public static Mesh BuildFlameMesh()
        {
            const int sides=20;
            float[] heights={.04f,.12f,.25f,.40f,.56f,.70f,.82f,.91f,.97f};
            float[] radii={.12f,.23f,.28f,.26f,.21f,.15f,.09f,.045f,.016f};
            var vertices=new System.Collections.Generic.List<Vector3>();
            var triangles=new System.Collections.Generic.List<int>();
            vertices.Add(Vector3.zero);
            for(int ring=0;ring<heights.Length;ring++)
            {
                float y=heights[ring];float bend=.18f*y*y*y;
                for(int j=0;j<sides;j++)
                { float a=2f*Mathf.PI*j/sides;vertices.Add(new Vector3(bend+radii[ring]*Mathf.Cos(a),y,radii[ring]*Mathf.Sin(a))); }
            }
            int tip=vertices.Count;vertices.Add(new Vector3(.20f,1.03f,0));
            for(int j=0;j<sides;j++)
            {
                int next=(j+1)%sides;
                triangles.Add(0);triangles.Add(1+j);triangles.Add(1+next);
                for(int ring=0;ring<heights.Length-1;ring++)
                {
                    int a=1+ring*sides+j,b=1+ring*sides+next,c=a+sides,d=b+sides;
                    triangles.Add(a);triangles.Add(c);triangles.Add(b);
                    triangles.Add(b);triangles.Add(c);triangles.Add(d);
                }
                int last=1+(heights.Length-1)*sides;
                triangles.Add(last+j);triangles.Add(tip);triangles.Add(last+next);
            }
            var result=new Mesh{name="ClosedToonFlame"};result.SetVertices(vertices);result.SetTriangles(triangles,0);
            result.RecalculateNormals();result.RecalculateBounds();
            result.bounds=new Bounds(new Vector3(0,.5f,0),new Vector3(2.4f,1.5f,2.4f));
            return result;
        }
        private static void StopLegacy(ParticleSystem ps)
        { if(ps!=null) { ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var r=ps.GetComponent<Renderer>();if(r!=null) r.enabled=false; } }
        public void SetHealth(int hp,int maximum,bool instant=false)
        {
            if(!Initialize()) return;
            maximum=Mathf.Max(1,maximum);hp=Mathf.Clamp(hp,0,maximum);
            if(!instant && hp==lastHP && maximum==lastMax) return;
            bool hurt=lastHP>=0 && hp<lastHP;
            lastHP=hp;lastMax=maximum;
            target=(float)hp/maximum; // Linear SIZE ratio: 50 HP / 100 HP = 50% scale.
            if(instant) { strength=target;hitRemaining=0f; }
            else if(hurt) hitRemaining=HitSeconds;
        }
        private void LateUpdate()
        {
            if(!initialized) return;
            hitRemaining=Mathf.Max(0f,hitRemaining-Time.deltaTime);
            // At zero HP blink first; then extinguish. Keep the component active for reset.
            if(lastHP!=0 || hitRemaining<=0f)
                strength=Mathf.MoveTowards(strength,target,Time.deltaTime/Mathf.Max(.05f,fadeSeconds));
            bool hit=hitRemaining>0f;
            float phase=1f-hitRemaining/Mathf.Max(.1f,HitSeconds);
            bool dim=hit && Mathf.FloorToInt(phase*Mathf.Max(1,blinkCount)*2f)%2==0;
            float gust=hit ? YutScaleImpactMotion.Gust(phase) : 0f;
            float pulse=1f+.025f*Mathf.Sin(Time.time*3.5f+transform.position.x);
            float size=Mathf.Max(.001f,strength)*worldHeight;
            art.transform.position=transform.position+Vector3.up*baseOffset;
            // Keep a stable yaw from the anchor root; never rotate to face the camera.
            art.transform.rotation=Quaternion.Euler(0,transform.eulerAngles.y,0);
            art.transform.localScale=new Vector3(size*widthRatio*pulse*(1f-.15f*gust),size/pulse*(1f-.20f*gust),size*widthRatio*pulse);
            material.SetFloat("_Brightness",dim?.26f:1f);
            material.SetFloat("_Gust",gust*hitWindBend*(warmPalette?-1f:1f));
            if(windViewCamera==null || !windViewCamera.isActiveAndEnabled) windViewCamera=Camera.main;
            Vector3 wind=windViewCamera!=null?windViewCamera.transform.right:Vector3.right;
            wind.y=0f;if(wind.sqrMagnitude<.001f)wind=Vector3.right;
            material.SetVector("_WindDirection",wind.normalized);
            material.SetFloat("_Warm",warmPalette?1f:0f);
            material.SetFloat("_Sway",swayAmount);
            material.SetFloat("_Speed",swaySpeed);
            foreach(var r in toonRenderers) r.enabled=strength>.001f;

        }
        private void OnEnable() { if(art!=null) art.SetActive(true);lastHP=lastMax=-1; }
        private void OnDisable() { if(art!=null) art.SetActive(false);if(glow!=null)glow.enabled=false; }
        private void OnDestroy() { if(material!=null) Destroy(material);if(mesh!=null) Destroy(mesh);if(art!=null)Destroy(art); }
        [ContextMenu("Tests/Preview Hit (Play Mode)")]
        private void TestHit() { if(Application.isPlaying) hitRemaining=HitSeconds; }
    }
}
