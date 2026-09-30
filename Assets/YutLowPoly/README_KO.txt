윷 로우폴리 에셋 — v1.2 (gzip 내부 이름 수정)
======================

1. 가장 간단한 설치
ZIP을 풀고 YutLowPoly.unitypackage를 Unity 프로젝트에서 열어 Import합니다.
스크립트 컴파일과 가져오기가 끝나면 다음 경로가 자동 생성됩니다.
Assets/YutLowPoly/Generated/
  Yut_Standard.prefab   일반 윷 1개
  Yut_BackDo.prefab     평평한 면에 검은 점이 있는 빽도 윷 1개
  Yut_Set_4.prefab      일반 3개 + 빽도 1개
  YutMesh.asset        공용 메시
  Wood.mat / Wood_BackDo.mat / Floor.mat
  Yut_Demo.unity        물리 던지기 데모

Yut_Demo를 열고 Play → 왼쪽 위 TOSS YUT 버튼으로 던집니다.
별도 입력 패키지 설치 없이 클릭으로 작동하도록 작성했습니다.
기존 장면에는 Yut_Set_4 프리팹을 드래그합니다.
바닥에는 3D Collider가 필요합니다. 윷이 바닥과 겹치지 않게 Y를 0.1 이상으로 배치하세요.

자동 생성되지 않으면 Console의 컴파일 오류를 먼저 해결한 후
Tools > Yut Low Poly > Create Missing Assets를 실행합니다.
설치 위치는 Assets/YutLowPoly를 유지해주세요. 설치 스크립트가 이 경로를 사용합니다.
이미 생성된 파일은 메뉴를 다시 실행해도 덮어쓰지 않습니다.

2. 대체 설치
unitypackage 대신 ZIP의 Assets/YutLowPoly 폴더를 프로젝트의 Assets 안에 복사할 수 있습니다.
두 방식을 동시에 사용할 필요는 없습니다.
Source 폴더의 OBJ/MTL과 Textures 폴더 PNG는 다른 3D 도구에서도 사용할 수 있습니다.
Unity 프리팹은 OBJ 임포터의 축 변환에 의존하지 않고, 동봉된 메시 데이터로 생성됩니다.

3. 모델 사양
- 윷 1개: 68 triangles / hard-edge split 포함 126 vertices
- 반원형 D 단면, 곡면 8분할, 양 끝 베벨
- 크기: 폭 0.056 m × 높이 0.028 m × 길이 0.30 m
- 원점: 중앙 부근 / 길이: 로컬 Z / 평평한 면의 바깥 방향: 로컬 +Y
- 1024×1024 RGB 나무 텍스처 2장 (일반/빽도)
- UV, 면 노멀 포함. X자와 검은 점은 텍스처 표현입니다.
- 공유 메시 + 재질 2개. 4개 세트 총 272 triangles
- 프리팹: Convex MeshCollider, Rigidbody(0.045 kg), 보간 및 연속 충돌 설정
- 크기를 바꿀 때는 XYZ를 같은 비율로 조정하세요.

4. 프로젝트 환경
Unity 2022.3 LTS / Unity 6 계열을 대상으로 작성했습니다.
Built-in Standard 또는 URP Lit(3D Renderer)을 자동 선택합니다.
HDRP/Lit도 검색하지만 HDRP 데모 환경은 별도 조명/노출 조정이 필요할 수 있습니다.
URP 2D Renderer 전용 설정에서는 3D Renderer를 사용하는 카메라/렌더링 구성이 필요할 수 있습니다.
처음 설치한 뒤 렌더 파이프라인을 바꿨다면 해당 파이프라인용 재질로 변환해주세요.

이 에셋은 3D Rigidbody / MeshCollider를 사용합니다.
2D 프로젝트에도 3D 모델을 표시할 수 있지만 Rigidbody2D / Collider2D와 물리 충돌하지 않습니다.
윷놀이의 말 이동, 턴 처리, 도/개/걸/윷/모 보상 규칙은 포함하지 않습니다.
기존 게임 규칙과는 별도로 사용할 수 있는 모델과 물리 데모입니다.
무작위 물리 시뮬레이션의 결과 확률은 균등하지 않으며 공정한 게임 확률을 보장하지 않습니다.

5. 앞뒤 판정 연결
YutLowPoly.YutStick 컴포넌트:
- isBackDo: 빽도 표시 윷인지
- IsSettled: Rigidbody가 잠들었는지
- FlatSideUp: 평평한 면이 위를 향하는지 (월드 Y 기준, dot > 0.7)
- RoundedSideUp: 둥근 면이 위를 향하는지 (dot < -0.7)
안정된 뒤 판정하세요. 두 값 모두 false면 옆으로 서 있거나 애매한 자세이므로
게임 규칙에 맞게 다시 던지기 등을 처리하세요.

6. 검증 범위
수행: 메시 인덱스/삼각형 면적/면 방향/닫힌 표면 검사, UV 텍스처 미리보기,
      unitypackage와 ZIP의 파일 구조 검사.
미수행: Unity Editor 실제 임포트, C# 컴파일, 플레이 모드 및 빌드 실행.
이 제작 환경에 Unity가 없어 위 엔진 검증은 수행하지 못했습니다.
Preview.png는 동봉된 실제 메시와 텍스처를 렌더링한 이미지이며 Unity 스크린샷은 아닙니다.

7. 수정 가능한 원본
OBJ, MTL, PNG, JSON, C#를 모두 포함합니다.
별도 유료 에셋이나 플러그인은 필요하지 않습니다.
Sources/build_geometry.py는 Python + NumPy + Pillow로 모델/텍스처를 재생성합니다.
Sources/render_preview.py는 동일 라이브러리로 미리보기를 렌더링합니다.
재생성하려면 두 스크립트를 ZIP 루트(Assets 옆)에 복사한 후 실행하세요.

참고 API 문서
https://docs.unity3d.com/ja/2022.3/ScriptReference/AssetDatabase.CreateAsset.html
https://docs.unity3d.com/ja/2022.2/ScriptReference/PrefabUtility.SaveAsPrefabAssetAndConnect.html

8. Import Unity Package 창이 비어 있을 때
패키지 창을 닫고 프로젝트를 저장한 뒤 Unity Editor를 종료합니다.
ZIP을 푼 폴더의 Assets/YutLowPoly와 Assets/YutLowPoly.meta를
프로젝트의 Assets 폴더 안으로 복사합니다. Assets/Assets로 중첩하지 마세요.
프로젝트를 다시 열어 가져오기/컴파일이 끝나면 Generated 폴더를 확인합니다.
기존 YutLowPoly 파일을 직접 수정했다면 덮어쓰기 전에 백업하세요.
이 버전은 unitypackage에 명시적 GUID 디렉터리와 폴더 메타데이터를 추가했습니다.
빈 창의 원인으로 확정한 것은 아니며 실제 Unity 재검증은 미수행입니다.

9. v1.2 수정
gzip 헤더의 내부 원본 이름을 archtemp.tar로 변경했습니다.
PackageImportTreeView.RecursiveComputeEnabledStateForFolders 오류와 동일한
공급사 지원 사례를 참고한 수정이며, Unity에서의 실행 검증은 아직 미수행입니다.
https://community.thebackend.io/t/sdk-import/11918
