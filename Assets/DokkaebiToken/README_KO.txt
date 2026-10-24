도깨비 목재 기물 / Dokkaebi Token

첨부 스케치의 둥근 방망이 머리, 뾰족한 목재 돌기, 원형 받침을 적용한 로우폴리 보드게임 기물입니다. 몸체/돌기/받침 모두 나무입니다.
삼각형 540개, 유니티에서 메시 1개/재질 1개로 결합됩니다. 512x512 나뭇결 텍스처 1장.
기본 높이 0.294m, 돌기를 포함한 폭 약 0.283m. 피벗=(0,0,0), 받침 바닥 중앙. 위쪽 +Y.

설치
1. ZIP을 압축 해제합니다.
2. DokkaebiToken.unitypackage를 Assets > Import Package > Custom Package로 Import합니다.
3. 컴파일이 끝나면 Assets/DokkaebiToken/Generated/DokkaebiToken.prefab이 자동 생성됩니다.
4. 자동 생성이 안 되면 Tools > Dokkaebi Token > Create Prefab을 누르세요.
5. 프리팹을 Hierarchy 또는 Scene으로 드래그합니다. Scene에서 선택 후 F를 누르면 찾아갈 수 있습니다.
6. 기물의 Position Y를 윷판 윗면 높이에 맞추세요. 기존 기물의 시각 모델을 교체한다면 기존 이동/선택 스크립트가 달린 부모 아래에 넣고 localPosition=(0,0,0), localRotation=(0,0,0)으로 맞추세요. 크기는 보드에 맞게 균일하게 조정합니다.

임포트 창이 회색이면
해당 창을 닫고 ZIP의 Assets/DokkaebiToken 폴더 전체를 프로젝트 Assets에 직접 복사하세요. 패키지 설치와 폴더 복사 중 하나만 사용합니다.
Tools 메뉴가 없으면 Console의 빨간 컴파일 오류를 먼저 확인하세요. 다른 스크립트 오류도 에디터 도구 실행을 막을 수 있습니다.
Source의 .png.bytes 파일은 의도한 형식입니다. 확장자를 바꾸지 마세요.

구성
Unity용 프리팹 생성 도구, 원본 OBJ/MTL/PNG, 실제 메시 미리보기, Python 제작 소스.
Built-in 및 URP용 재질을 자동 선택합니다. HDRP는 별도 재질 설정이 필요합니다.
프리팹은 정적 외형과 간단한 CapsuleCollider만 포함하며 Rigidbody/게임 규칙/이동 스크립트는 없습니다. 기존 게임과 자동 연동되지 않습니다.
Collider는 클릭/선택을 위한 단순화된 형태로 돌기 모양을 정확히 따르지 않습니다. 기존 부모에 Collider가 있으면 필요에 맞게 중복 Collider를 제거하세요.
재질과 생성된 에셋은 메뉴를 다시 눌러도 덮어쓰지 않습니다. 파이프라인 전환 후에는 재질 변환 또는 백업 후 Generated 재생성이 필요합니다.
미리보기는 실제 메시의 별도 렌더 이미지입니다. Unity에서는 조명에 따라 달라집니다.
메시 인덱스/면적과 이미지/압축파일 구조 검사를 했습니다. 실제 Unity Editor 환경이 없어 컴파일/실행 테스트는 하지 못했습니다.
