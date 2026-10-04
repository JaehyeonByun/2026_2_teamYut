영혼의 천칭 / Soul Scale — Low Poly Rig v1

1. 구성과 적용
ZIP을 풀고 SoulScale.unitypackage를 Unity에 Import합니다.
컴파일 후 Assets/SoulScale/Generated/SoulScale.prefab이 생성됩니다.
생성되지 않으면 Tools > Soul Scale > Create Missing Assets를 실행하세요.
패키지 임포트에 문제가 있으면 ZIP의 Assets/SoulScale 폴더와 SoulScale.meta를
프로젝트 Assets 폴더로 복사해도 됩니다. 두 설치 방법 중 하나만 사용하세요.
기존 에셋이나 씬을 덮어쓰지 않습니다. 자동 생성 메뉴는 기존 프리팹을 보존합니다.
Generated/SoulScale.prefab을 현재 게임 씬에 드래그합니다.

2. 책상에 배치
원점은 천칭 받침대 바닥 중앙입니다.
원래 크기: 가로 1.64 / 높이 1.40 / 깊이 0.47 Unity 단위.
처음에는 프리팹 최상위 Scale (0.5, 0.5, 0.5)를 추천합니다.
이 경우 가로 0.82 / 높이 0.70 / 깊이 0.235입니다.
책상 상판 월드 Y가 0.825라면 프리팹 Position Y는 0.825로 놓습니다.
예: 책상 중심 (0,0,0), 상판 폭 4.8, 깊이 2.4라면
Position (1.25, 0.825, 0.45), Rotation (0,0,0), Scale (0.5,0.5,0.5)부터 조절합니다.
기본 카메라에서 얼굴/윷판과 겹치지 않게 X/Z를 조절하세요.
균일 Scale만 사용하고, 프리팹 최상위는 세워 둔 상태에서 Y축 방향만 돌리세요.
회전하는 본과 메시 자식에 별도 Scale을 적용하지 마세요.

3. 실제 본 구조 (4 bones)
Root: 고정 받침대/기둥/상단 장식
Beam: 중앙 원 두 개 + 양팔. 가운데 축을 기준으로 함께 회전
Hang_L: 왼쪽 고리 + 줄 세 개 + 접시
Hang_R: 오른쪽 고리 + 줄 세 개 + 접시
Hang_L과 Hang_R은 Beam의 자식 본이며, 팔 끝이 회전하면 매달린 위치가 따라갑니다.
모든 정점은 한 본에 100% 가중치로 연결해 금속/줄 형태가 늘어나지 않게 했습니다.
줄 자체의 휘어짐/천 시뮬레이션은 포함하지 않습니다. 줄과 접시는 하나의 강체처럼 흔들립니다.
중앙 원은 회전 대칭이라 회전이 눈에 덜 띄지만 실제로 Beam 본에 스키닝되어 있습니다.

4. 기울기 테스트
Play를 누르고 SoulScale 최상위의 SoulScaleRigController를 확인합니다.
컴포넌트 제목 우클릭 > Tests > Left Wins (Play Mode): 왼쪽 아래로 기울기
Tests > Right Wins (Play Mode): 오른쪽 아래로 기울기
Tests > Reset (Play Mode): 수평 초기화
Target Angle: -18 ~ +18도. 양수는 로컬 X가 음수인 왼쪽 접시가 내려갑니다.
Smooth Seconds: 기본 0.7, 값이 클수록 천천히 기울어집니다.
Gentle Sway: 켜면 접시가 작게 흔들립니다. 끄면 항상 천칭 기준 수평입니다.
카메라를 뒤로 돌리면 화면상의 좌우는 반대가 됩니다. L/R은 모델의 로컬 X 기준입니다.
Edit Mode에서 본을 미리 기울인 채 저장하지 말고 기본 포즈에서 시작하세요.
본 회전을 쓰는 Animator와 이 제어 스크립트를 동시에 실행하지 마세요.

5. 게임 연결용 메서드
using SoulScaleAsset;
[SerializeField] private SoulScaleRigController soulScale;

플레이어를 L에 배치했다면 승리 시 soulScale.SetLeftWinner();
상대 승리 시 soulScale.SetRightWinner();
리셋 시 soulScale.ResetScale();
임의 각도: soulScale.SetAngle(10f);
이번 파일은 모델/리그 및 동작 제어만 제공합니다.
기존 YutTurnManager나 체력 코드를 자동 수정하지 않습니다.
실제 체력 시스템 연결은 해당 승패 판정이 만들어진 뒤 위 메서드를 호출하세요.

6. 영혼불 부착 위치
FlameAnchor_L, FlameAnchor_R이 각 접시 바닥 중앙 바로 위에 배치되어 있습니다.
각 Anchor 아래에 불 이펙트를 넣고 Local Position (0,0,0)부터 맞추세요.
불 이펙트/체력 UI는 이번 패키지에 포함되지 않습니다.
코드에서는 FlameAnchorLeft / FlameAnchorRight 프로퍼티로 접근할 수 있습니다.
앵커는 접시의 흔들림을 따라갑니다. 불을 항상 월드 위쪽으로 유지하려면
불 이펙트 쪽에서 월드 방향을 유지하도록 별도 제어하세요.

7. 편집용 파일
Source/SoulScale_Rig.blend: Blender 4.3.2에서 작성, 실제 Armature/가중치 포함.
팔레트 텍스처는 Blender 원본에 패킹되어 있습니다.
1~130 프레임의 Tilt_Demo_LevelPans 액션: 양팔 기울기 + 접시 역회전 키 예제.
Beam만 수동 회전하면 자식 접시도 기울므로, 접시 본을 역회전하거나 제공 액션을 사용하세요.
Source/SoulScale_Rig.glb: 스킨/본 포함 교환용 파일. Unity 기본 설치에는 GLB 임포터가 없으므로 FBX를 사용하세요.
Assets/SoulScale/Models/SoulScale_Rig.fbx: Unity용 기본 포즈, 스킨/본/앵커 포함.
FBX에는 자동 재생 애니메이션을 넣지 않았습니다. 기본 제공 제어 스크립트가 본을 움직입니다.
Source/build_soulscale.py: 모델, 리그, 렌더를 재생성하는 Blender Python 원본.
Source/verify_fbx.py: FBX를 다시 임포트하고 본/앵커/접시 수평을 검사하는 Blender Python.

8. 사양 및 검증
삼각형 3,652개 / 원본 메시 정점 1,884개 / 본 4개 / 메시 1개 / 재질 1개.
Unity의 UV/하드 노멀 분할 이후 정점 수는 달라질 수 있습니다.
짙은 청동색 + 밝은 청동 테두리 + 어두운 줄. 단순 팔레트 텍스처 포함.
Blender에서 실제 스키닝 후 -18/0/+18도 자세의 접시 수평, 바닥 간섭 여유를 검사했습니다.
FBX 내보내기 후 다시 임포트하여 본 4개, 앵커 좌표, 세 자세의 접시 수평을 재검증했습니다.
Preview의 PNG/GIF는 실제 모델을 렌더링한 결과이며 Unity 스크린샷이 아닙니다.
Unity Editor에서 실제 임포트/C# 컴파일/플레이 검증은 이 환경에서 수행하지 못했습니다.
참조 대상: Unity 2022.3/Unity 6, Built-in 또는 URP 3D Renderer.
HDRP 셰이더 감지는 포함하지만 HDRP 환경의 조명/노출은 별도 조정이 필요할 수 있습니다.
Rigidbody/Collider/물리 관절은 포함하지 않습니다. 천칭은 스크립트로 제어됩니다.

9. 수동 FBX 임포트 시
Rig = Generic, Optimize Game Objects = Off, Import Animation = Off로 설정하세요.
Optimize Game Objects를 켜면 스크립트가 접근할 본 Transform이 숨겨질 수 있습니다.
https://docs.unity.com/en-us/engine/6000.0/manual/assets-and-media/asset-types/models/importing/fbximporter-rig
https://docs.blender.org/api/4.3/bpy.ops.export_scene.html
