거친 나무 책상 / Rough Wood Table v1

[빠른 적용]
1. ZIP을 풀고 RoughWoodTable.unitypackage를 Unity에서 Import합니다.
2. 컴파일이 끝나면 Assets/RoughWoodTable/Generated 폴더를 엽니다.
3. RoughWoodTable.prefab을 Hierarchy로 드래그합니다.
4. 씬을 저장합니다. Prefab은 바닥 중앙이 원점이므로 Y=0이면 발바닥이 Y=0에 놓입니다.

자동 생성되지 않으면 Tools > Rough Wood Table > Create Missing Assets를 실행합니다.
Console에 컴파일 오류가 있다면 먼저 해결해야 합니다.
패키지 창에 문제가 생기면 Unity를 닫고 ZIP의 Assets/RoughWoodTable 폴더와
RoughWoodTable.meta를 프로젝트 Assets 안에 복사한 뒤 프로젝트를 다시 엽니다.
패키지 방식과 폴더 복사 방식 중 하나만 사용하면 됩니다.

[구성]
Generated/RoughWoodTable.prefab : 배치용 프리팹
Generated/TableMesh.asset : 책상 메시
Generated/WoodTexture.asset : PNG에서 생성한 Unity 텍스처
Generated/RoughWood.mat : 거친 나무 재질
Editor/RoughWoodTableSetup.cs : 에디터 전용 생성 스크립트
Source/TableMesh.json : 정확한 메시 원본 데이터
Source/RoughWoodTable_Albedo.png.bytes : 설치용 PNG 바이트

ZIP의 Models 폴더에는 다른 3D 도구에서도 쓸 수 있는 OBJ/MTL/PNG가 있습니다.
OBJ를 열 때 MTL과 PNG도 같은 폴더에 두세요.
Sources에는 모델 생성, 미리보기 렌더링, 패키징 Python 원본이 있습니다.
재생성에는 Python, NumPy, Pillow가 필요합니다. Unity 사용에는 Python이 필요 없습니다.

[요청 반영]
- 사용자 스케치의 두꺼운 상판과 네 개의 각진 다리
- 가로 X 1.60m / 세로 Z 0.80m / 높이 Y 0.65m (상판 비율 2:1)
- 상판 두께 0.08m, 조금씩 불규칙하게 깎인 모서리
- 4개 판자를 붙인 듯한 이음선, 거친 결, 작은 긁힘과 옹이
- 판자 틈은 텍스처 표현. 실제 판자는 분리하지 않았습니다.
- 상판과 다리까지 하나로 연결된 닫힌 표면 (1 connected component)
- 206 triangles / UV와 하드 노멀 분할 포함 353 render vertices
- 메시 1개, 재질 1개, 1024x1024 컬러 텍스처 1장
- GameObject 1개. 분리된 판자나 다리 자식 오브젝트 없음

[충돌과 게임 연결]
상판 1개 + 다리 4개를 근사한 3D BoxCollider 5개가 같은 오브젝트에 붙습니다.
상판 윗면은 로컬 Y=0.65입니다. Rigidbody가 있는 3D 물체를 올릴 수 있습니다.
모서리의 작은 베벨과 콜라이더 형태는 정확히 일치하지 않을 수 있습니다.
Rigidbody와 런타임 스크립트는 없습니다. 배경 가구로 정지해 있습니다.
윷놀이 턴/말 이동 등 기존 게임 규칙과 자동 연결되지 않습니다.
2D Collider와는 물리 충돌하지 않습니다.

[재질과 버전]
Unity 2022.3 / Unity 6의 Built-in Standard 및 URP Lit(3D Renderer)을 대상으로 작성했습니다.
HDRP를 감지하면 HDRP/Lit을 선택하지만 별도 환경 검증은 하지 않았습니다.
URP 2D 전용 Renderer에서는 3D 모델 렌더링 설정을 별도로 맞춰야 할 수 있습니다.
Smoothness=0, Metallic=0으로 설정합니다. Normal map은 사용하지 않습니다.
설치 후 렌더 파이프라인을 바꾸면 재질 셰이더도 변환해야 합니다.

Editor 스크립트가 자신의 위치를 찾아 실행하므로 RoughWoodTable 폴더 전체를
Assets 아래 다른 폴더로 옮겨도 사용할 수 있습니다. 내부 Editor/Source 구조는 유지하세요.
만들어진 파일은 재실행 시 덮어쓰지 않습니다. 같은 에셋 폴더를 여러 개 복제하지 마세요.

[검증]
모든 삼각형 면적, 인덱스, UV 범위, 바깥 면 방향, 연결성, 닫힌 표면,
치수와 비율, PNG 디코딩, OBJ/JSON 일치, ZIP/패키지 구조를 검사했습니다.
Preview.png는 실제 메시와 텍스처를 렌더링한 이미지이며 Unity 화면 캡처는 아닙니다.
이 환경에는 Unity Editor가 없어 C# 컴파일/실제 임포트/플레이 검증은 미수행입니다.
