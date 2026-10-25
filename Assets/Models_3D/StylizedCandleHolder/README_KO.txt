로우폴리 카툰풍 촛대 / Stylized Candle Holder v2 - 반전 무늬판

1. 설치
ZIP을 풀고 StylizedCandleHolder.unitypackage를 Unity에서 Import합니다.
컴파일 후 Assets/StylizedCandleHolder/Generated가 자동 생성됩니다.
StylizedCandleHolder.prefab을 Hierarchy로 드래그하세요.
생성되지 않으면 Tools > Stylized Candle Holder > Create Missing Assets를 실행하세요.
프로젝트의 Console 컴파일 오류가 있다면 먼저 해결해야 합니다.

직접 복사 설치: Unity를 닫고 ZIP 안의 Assets/StylizedCandleHolder 폴더와
StylizedCandleHolder.meta를 프로젝트 Assets 안으로 복사하고 프로젝트를 다시 엽니다.
두 설치 방법 중 하나만 사용합니다. 폴더 내부의 Editor/Source 구조를 유지하세요.
설치 스크립트가 자기 위치를 찾으므로 폴더 전체는 Assets 아래 다른 폴더로 옮겨도 됩니다.

2. 모델
첫 번째 사용자 스케치를 중심으로 한 원형 받침, 긴 촛대 기둥, 초 받침과 아이보리 초,
둥근 차광판, 뒤쪽 기둥, 아치형 연기 덮개, 얇은 장식 격자를 포함합니다.
두 번째 사진의 어두운 청동 분위기를 밝은 청동/황동 계열로 단순화했습니다.
촛농과 심지 포함. 불꽃 메시, Particle System, Light는 포함하지 않습니다.
원형 차광판은 속이 찬 금속 판이며 거울/반사 셰이더가 아닙니다.
무늬판은 빈 배경을 채운 청동 판입니다. 중앙 십자와 네 모서리 ㄱ자 무늬는 실제 관통 구멍입니다.
외곽 테두리는 유지하며, 이전 무늬의 얇은 수평 연결 막대는 제거했습니다.

전체 1,142 triangles, 하드 노멀/UV 분할 포함 2,024 render vertices.
MeshRenderer 1개, 메시 1개, 사용 재질 1개, 128x80 색상 팔레트 1장.
실루엣을 살린 다각형 면과 단계별 색으로 카툰 느낌을 냈습니다.
전용 외곽선/셀 셰이더는 사용하지 않습니다.
바닥 중앙 원점, +Y 위, 정면 -Z. 높이 약 0.849m, 폭 0.294m, 깊이 0.301m.
책상 위에 놓으려면 루트 Position Y를 책상 윗면에 맞추세요.
게임 내 크기는 XYZ Scale을 같은 비율로 조절하세요.

3. 불 이펙트 붙이기
Hierarchy의 촛대를 펼쳐 FlameAnchor를 선택하세요.
사용할 불꽃 Particle System 또는 VFX 프리팹을 FlameAnchor의 자식으로 넣습니다.
이펙트 Local Position=(0,0,0), Local Rotation=(0,0,0)으로 맞춘 후 크기/방향을 조절하세요.
Anchor 로컬 위치는 (0.001, 0.563, -0.035)이며 심지 바로 위입니다. 위쪽은 +Y입니다.
이펙트 자체 원점과 방출 방향에 따라 오프셋이 필요할 수 있습니다.
Point Light를 사용한다면 같은 Anchor 아래 배치하고 강도/범위를 조절하세요.
자식 Particle System은 Simulation Space=Local이면 촛대 이동/회전을 따라갑니다.

4. 재질
기본 Candle_StylizedLit.mat은 장면 조명과 Point Light에 반응합니다.
어두운 장면에서도 미리보기처럼 일정한 색을 원하면 Mesh Renderer의 재질 슬롯에
Candle_AlwaysVisible.mat을 넣으세요. 이 Unlit 재질은 조명/Point Light에 반응하지 않습니다.
밝은 테두리는 팔레트 색이며 실제 금속 반사나 발광은 아닙니다.
기본 셰이더는 Built-in Standard / URP Lit, 선택 Unlit은 Built-in Unlit/Texture / URP Unlit.
HDRP 셰이더 탐색도 포함했지만 HDRP 검증은 미수행입니다.
Unity 2022.3 / Unity 6 계열을 대상으로 작성했습니다. URP는 3D Renderer를 사용하세요.
파이프라인을 나중에 바꿨다면 재질도 해당 파이프라인 셰이더로 변환해야 합니다.

5. 동작/충돌
런타임 스크립트나 Rigidbody는 없습니다. 고정된 배경 소품입니다.
정적 non-convex MeshCollider가 있어 3D 물체와 충돌할 수 있습니다.
물리적으로 떨어뜨리거나 들고 움직이려면 단순한 Box/Capsule 조합 또는 convex collider로
충돌체를 바꾼 뒤 Rigidbody를 추가하세요. 기존 게임 규칙과 자동 연결되지 않습니다.
콜라이더가 필요 없으면 MeshCollider를 제거해도 됩니다.

6. 원본/검증
Models 폴더에 OBJ/MTL/PNG 원본이 있습니다. 같은 폴더에 두고 사용하세요.
Sources에는 Python 제작 및 패키징 코드가 있습니다. Python+NumPy+Pillow+SciPy로 재생성할 수 있습니다.
Unity 사용에는 Python이나 별도 외부 플러그인이 필요하지 않습니다.
메시 인덱스/삼각형 면적/면 방향/UV/PNG/OBJ 일치/압축 구조를 검사했습니다.
Preview.png는 실제 메시의 렌더이며 Unity 화면 캡처가 아닙니다.
Unity Editor가 없는 환경에서 제작해 실제 임포트, C# 컴파일, 플레이/빌드 검증은 미수행입니다.

7. v2 기존 설치 업데이트
새 패키지를 같은 프로젝트에 Import하고 컴파일이 끝나면 자동으로 기존 CandleMesh.asset를 갱신합니다.
변경이 바로 보이지 않으면 Tools > Stylized Candle Holder > Apply Inverted Panel Update를 누르세요.
Generated 폴더/프리팹을 삭제하지 마세요. 기존 메시의 식별자를 유지해 배치된 프리팹에도 반영합니다.
프리팹 자체와 FlameAnchor, 사용자가 연결한 불꽃 이펙트는 덮어쓰지 않습니다.
메시 자체를 직접 수정했다면 업데이트 전에 해당 Mesh asset을 별도로 백업하세요.
오브젝트를 Unpack하거나 메시를 별도 복제한 경우에는 원본 메시 참조 여부를 확인하세요.
앞/뒷면 면적 검사로 다섯 구멍을 덮는 삼각형이 없는지 검증했습니다.
Unity 실행 검증은 미수행입니다.
