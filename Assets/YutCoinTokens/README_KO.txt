윷 듀얼 동전형 기물 — Yut Coin Tokens

아군: 청록/하늘색 영혼불과 원형 중심이 있는 불 문양.
상대: 붉은 갈색/주황 테두리와 황금색 도깨비방망이 문양.
24각 동전, 깎인 모서리, 낮은 안쪽 면, 얕은 부조 문양입니다.
문양은 텍스처가 아니라 실제 메시입니다. 관통 구멍이나 음각 Boolean 가공은 아닙니다.

설치
1. ZIP을 압축 해제하세요.
2. Assets/YutCoinTokens 폴더 전체를 프로젝트 Assets 안에 복사하세요.
3. Unity 컴파일을 기다리면 Generated 폴더에 메시, 재질, 프리팹을 자동 생성합니다.
4. 생성되지 않으면 Tools > Yut Coin Tokens > Create Assets를 누르세요.
5. Generated/Ally_Coin.prefab 또는 Enemy_Coin.prefab을 씬으로 드래그하세요.
대안으로 포함된 YutCoinTokens.unitypackage를 가져올 수 있습니다.
Import Package 창이 회색으로 멈추면 폴더 복사 방식을 사용하세요. 두 방식 중 하나만 사용합니다.

기존 기물에 외형 적용
현재 기물의 이동/선택/업/피격 스크립트를 그대로 유지한 채, 새 프리팹 내부 Visual 자식을
기존 기물 루트의 자식으로 옮겨 사용할 수 있습니다. 이전 외형 Renderer를 끄고 새 Visual의
Local Position과 Rotation을 0, Scale을 1로 맞춘 후 보드 크기에 맞춰 균등 스케일을 조절하세요.
이 킷에는 게임 이동, 선택, 불타기, 외곽선, 업 기능을 연결하는 스크립트가 포함되지 않습니다.
선택/외곽선 시스템이 참조하는 Renderer는 새 모델의 모든 자식 Renderer로 다시 지정해야 합니다.
기존 선택 콜라이더가 있다면 중복 콜라이더를 추가할 필요는 없습니다.

규격
1 Unity unit = 1m. 지름 .094m, 높이 .016m. +Y 위쪽, 문양 머리는 +Z 방향.
피벗은 바닥 중심이며 보드에 눕혀 놓는 형태입니다. 바닥이 보드 표면에 닿도록 Y를 맞추세요.
아군 769 triangles, 상대 777 triangles. 각 4재질/5개 메시 파트, 이미지 텍스처 없음.
뒷면은 무늬가 없는 단색 바닥입니다. 두께/색은 프리팹 스케일과 재질에서 조절 가능합니다.
루트에 선택용 BoxCollider 포함. Rigidbody는 포함하지 않았습니다.
StackSocket은 루트 기준 Y=.017m의 빈 Transform이며 다음 기물의 바닥 배치 기준입니다.
StackSocket 자체가 업 기능을 실행하지는 않습니다.

재질
Built-in / URP를 자동 감지해 3단계 카툰 명암 셰이더를 생성합니다. HDRP는 지원하지 않습니다.
불 색상은 재질색이며 실제 파티클, 발광, Bloom 또는 주변을 비추는 조명은 포함하지 않습니다.
재질 Inspector에서 색, 그림자 단계, 명암 경계를 조절할 수 있습니다.
광원과 환경광에 따라 색과 명암이 달라집니다. 커스텀 셰이더는 베이크 라이트맵을 지원하지 않습니다.
URP 보조 광원은 Per Pixel에서 단계 명암이 잘 드러납니다.
분홍색으로 표시되면 Console의 첫 셰이더 오류와 Unity/URP 버전을 확인하세요.
필요시 프로젝트의 Lit/Standard 재질로 바꾸면 기본 표시가 가능하나 카툰 단계 명암은 사라집니다.
기존 Generated 재질/프리팹은 재실행 시 보존합니다. 파이프라인 변경 후에는 Create Assets로
해당 셰이더를 생성하고 각 재질의 Shader와 색을 수동으로 갱신하세요.

파일 및 확인 범위
Models에는 OBJ/MTL 교환용 모델을 포함했습니다. Unity에서는 자동 생성 프리팹 사용을 권장합니다.
Sources/build.py, preview.py, package.py는 제작 소스이며 Unity 사용에 Python은 필요하지 않습니다.
Preview.png는 실제 생성 메시를 별도 렌더러로 그린 참고 이미지이며 Unity 스크린샷이 아닙니다.
메시의 유효 인덱스/노멀/삼각형 면적 및 패키지 압축 구조를 확인했습니다.
Unity Editor 실행 환경이 없어 실제 임포트 및 셰이더 컴파일은 검증하지 못했습니다.
