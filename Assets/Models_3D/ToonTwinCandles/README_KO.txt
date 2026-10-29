Toon Twin Candles — 유니티용 로우폴리 초 세트

1. 설치 (권장: 폴더 복사)
ZIP을 먼저 압축 해제하세요. 압축 안의 Assets/ToonTwinCandles 폴더 전체를
사용 중인 프로젝트의 Assets 폴더 안에 복사합니다.
Unity로 돌아가 스크립트 컴파일과 자동 생성을 기다립니다.
Project 창 Assets/ToonTwinCandles/Generated에 프리팹이 생성됩니다.
자동 생성이 안 되면 상단 Tools > Toon Twin Candles > Create Assets를 누르세요.
다른 스크립트의 컴파일 오류가 있으면 먼저 해결해야 합니다.

대안: ToonTwinCandles.unitypackage를 Assets > Import Package > Custom Package로
가져올 수도 있습니다. 두 방식 중 하나만 사용하세요.
Import Package 창이 회색으로 멈추면 창을 닫고 폴더 복사 방식을 사용하세요.
Models 폴더의 OBJ는 원본 교환용입니다. FlameSocket과 콜라이더는 Unity 프리팹에 포함됩니다.

2. 씬에 넣기
Generated/TwinCandles.prefab을 Hierarchy 또는 Scene 창으로 드래그하세요.
선택 후 F를 누르면 Scene 카메라가 해당 모델에 맞춰집니다.
씬의 카메라와 조명이 모델을 향하도록 배치하세요. 모델 자체에는 조명이 없습니다.
개별 초: Candle_Short.prefab / Candle_Tall.prefab
초 없는 촛대: TwinHolder_Empty.prefab
모델 바닥 중심이 피벗이고 Y축이 위입니다. 1 Unity unit = 1m 기준입니다.
작게 보이면 프리팹 루트의 Scale을 XYZ 동일한 비율로 조정하세요.

3. 불꽃 이펙트 연결
Hierarchy에서 TwinCandles를 펼치면 다음 자식들이 있습니다.
  Candle_Short / FlameSocket
  Candle_Tall / FlameSocket
원하는 Particle System 또는 불꽃 프리팹을 FlameSocket 아래 자식으로 넣으세요.
불꽃 자식의 Local Position=(0,0,0), Local Rotation=(0,0,0)으로 설정합니다.
불꽃 크기는 사용하는 이펙트에 맞춰 조절하세요. 기준점의 +Y가 위쪽입니다.
마킹은 Scene 창 Gizmos를 켜면 주황색 구와 십자, 이름으로 표시됩니다.
이 표시만으로 불꽃이 렌더링되지는 않으며 게임 화면에는 나타나지 않습니다.
실제 불꽃, 발광, Point Light, 점화 동작은 포함하지 않았습니다.
개별 초 프리팹도 내부 Candle_Short 또는 Candle_Tall 아래 FlameSocket이 있습니다.
빈 촛대의 CandleSeat_Left / Right는 초 바닥을 놓는 기준점입니다.

4. 형태와 사양
짧은 초: 몸통 18cm, 지름 7cm. 녹은 윗면과 3개의 촛농 표현. 312 triangles.
긴 초: 몸통 28cm, 지름 5.2cm. 얕게 파인 윗면과 심지. 168 triangles.
촛대: 짙은 금속색과 황동색 포인트. 780 triangles.
조립 세트: 총 1,260 triangles, 재질 4개, 텍스처 이미지 없음.
조립 세트 너비 약 42cm, 높은 쪽 심지까지 약 63.3cm.
불꽃 기준점: 단독 초 기준 Y=.194 / .294m.
조립 세트 루트 기준 (-.15,.535,0) / (.15,.635,0)m.
초에는 CapsuleCollider, 촛대에는 단순 BoxCollider가 있습니다.
Rigidbody는 없으며 정적 배치용입니다. 물리적으로 넘어지게 하려면 루트에 추가하세요.

5. 카툰 재질과 렌더 파이프라인
Built-in 또는 URP를 감지해 해당 카툰 셰이더를 생성합니다. HDRP는 지원하지 않습니다.
광원 방향에 따라 3단계 명암으로 표현하며, 재질 Inspector에서 색과 명암 단계를 조절합니다.
URP의 보조 광원은 Per Pixel 설정에서 단계 명암이 더 잘 드러납니다.
환경광이 강하면 명암 차이가 작아질 수 있습니다. 베이크 라이트맵은 이 셰이더에서 지원하지 않습니다.
기존 생성 재질과 프리팹은 재실행으로 덮어쓰지 않습니다.
생성 후 렌더 파이프라인을 바꿨다면 Create Assets로 새 셰이더를 생성한 뒤,
Generated의 각 재질 Shader를 ToonTwinCandles/BuiltIn 또는 ToonTwinCandles/URP로 바꾸세요.
색상도 확인하세요. 서로 사용하는 색상 프로퍼티가 다릅니다.
분홍색이거나 생성이 중단되면 Console의 첫 셰이더 오류와 Unity/URP 버전을 확인하세요.
긴급히 표시만 하려면 프로젝트 표준 Lit/Standard 재질로 교체할 수 있으나 단계 명암은 사라집니다.

6. 파일 및 검증 범위
게임의 다른 스크립트나 윷놀이 로직과 자동 연결되지 않습니다.
Editor 폴더는 에셋 생성용이며 FlameSocketMarker는 편집 화면의 기준점 표시용입니다.
Sources에는 재생성용 Python 코드가 있습니다. Unity에서 사용하기 위해 Python을 설치할 필요는 없습니다.
Preview.png는 실제 생성 메시로 만든 참고 렌더이며 Unity 스크린샷은 아닙니다.
메시 인덱스, 노멀 방향, 삼각형 수, 압축 파일 구조를 검증했습니다.
Unity Editor가 없는 환경에서 제작하여 실제 Unity 임포트 및 셰이더 컴파일은 실행 검증하지 못했습니다.
