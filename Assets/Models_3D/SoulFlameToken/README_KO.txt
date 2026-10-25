영혼불 기물 / Soul Flame Token

스케치를 참고한 로우폴리 3D 기물입니다. 바깥 불꽃/작은 안쪽 불꽃/둥근 중심을 앞뒤로 겹쳐 측면에서 층이 보이도록 만들었습니다.
외층과 내층은 두께 있는 조각이며, 중심은 납작한 구형입니다. 뒷면도 막혀 있습니다. 가장 잘 보이는 정면은 -Z 방향입니다.
청록색은 기본 제안 색상입니다. Generated 폴더의 Base, Outer, Inner, Core 재질 색상을 각각 변경할 수 있습니다.
삼각형 640개, 메시 4개, 재질 4개. Unity에서는 텍스처 없이 색상 재질만 사용합니다.
높이 0.315m, 최대 폭 약 0.218m. 피벗은 받침 바닥 중앙, 위쪽 +Y.

설치
1. ZIP 압축을 풀고 SoulFlameToken.unitypackage를 Import합니다.
2. 컴파일이 끝나면 Assets/SoulFlameToken/Generated/SoulFlameToken.prefab이 생성됩니다.
3. 안 생기면 Tools > Soul Flame Token > Create Prefab을 실행합니다.
4. 프리팹을 Hierarchy로 끌어 놓고 Scene에서 선택 후 F를 누르면 모델을 찾을 수 있습니다.
5. 작업 중인 윷판의 윗면 높이에 Position Y를 맞추세요. 기물 크기는 Scale X/Y/Z를 같은 비율로 조절하세요.
6. 기존 기물의 이동/선택 기능을 유지하려면 기존 부모 오브젝트 아래에 시각 모델로 배치하고 localPosition 및 localRotation을 0으로 맞추세요. 기존 부모 Collider와 중복될 경우 이 프리팹의 Collider는 필요에 따라 제거합니다.

임포트 문제 시
회색 Import 창이면 창을 닫고 ZIP 안의 Assets/SoulFlameToken 폴더 전체를 프로젝트 Assets에 복사하세요. 두 설치 방법 중 하나만 사용합니다.
Tools 메뉴가 없으면 Console의 빨간 컴파일 오류부터 확인하세요. 다른 스크립트 오류도 도구 실행을 막을 수 있습니다.
생성 도구는 기존 프리팹/재질을 덮어쓰지 않습니다. 변경한 파일을 백업한 다음 Generated를 삭제하고 메뉴를 다시 실행하면 재생성됩니다.

기능과 범위
Built-in/URP 프로젝트에 맞는 Lit 재질을 선택합니다. HDRP는 별도 재질 설정이 필요합니다.
기본은 정적인 불꽃 조각입니다. 흔들리는 불 애니메이션/파티클/디졸브/빛 번짐 효과는 포함하지 않습니다.
게임 로직 및 이동 스크립트는 자동 연결하지 않습니다. Collider는 클릭 판정용 단순 BoxCollider입니다. Rigidbody는 없습니다.
발광을 원하면 Inner/Core 재질의 Emission과 프로젝트의 Bloom을 별도로 설정하세요.
Models에는 다른 3D 편집기에 사용할 수 있는 OBJ/MTL, Sources에는 제작 Python 소스가 들어 있습니다. Python은 Unity 설치 시 필요하지 않습니다.
미리보기는 실제 메시를 별도 렌더링한 이미지입니다. Unity 조명에 따라 결과가 달라집니다.
메시 인덱스/면적/법선, 압축파일을 검사했습니다. 제작 환경에 Unity Editor가 없어 실제 Unity 컴파일/플레이 검증은 하지 못했습니다.
