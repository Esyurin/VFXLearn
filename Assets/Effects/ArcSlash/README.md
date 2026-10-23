# ArcSlash — 첫 원호

비대칭 흰 원호의 형태와 한 번의 회전·페이드 재생을 확인하는 에셋입니다.

## 폴더 구성

에셋은 종류별 하위 폴더에 있습니다. 아래 설명은 파일 이름으로만 적습니다.

| 폴더 | 내용 |
|---|---|
| `Meshes/` | ArcRibbon, TangentRibbon, ArcDashRibbon, GroundRing, SlashMesh.fbx |
| `Textures/` | DashNoise, DotCircle, DotGlow, RingLine, RingNoise, StreakNoise, StreakSoft, Noise Texture |
| `Shaders/` | ArcSlash.shadergraph, SlashShader.shadergraph |
| `Materials/` | ArcSlash_White, SlashShader |
| `VFX/` | ArcSlash.vfx, ArcSlash_ParticleDash.vfx, SlashDistortion.vfx |
| `Prefabs/` | ArcSlash, ArcSlash_ParticleDash, ArcSlash_StaticPreview |
| `Scenes/` | ArcSlash_Preview |

- `ArcSlash.vfx`: Single Burst 1개 → Initialize → Update → Shader Graph Mesh 출력.
- `ArcSlash.prefab`: 재생용 VisualEffect 프리팹. 활성화 시 기본 OnPlay 이벤트로 한 번 재생합니다.
- `ArcSlash_Preview.unity`: 카메라와 조명을 포함한 독립 미리보기 씬. 씬을 열고 Play Mode에서 재생하거나 VisualEffect의 Play/Reinit으로 다시 재생합니다.

- `ArcRibbon.asset`: XY 평면의 220도 원호 띠. 중심 반경 1, 띠 폭 0.3, 128분할. UV의 U=0은 긴 꼬리, U=1은 앞쪽 끝입니다.
- `ArcSlash.shadergraph`: URP Unlit / Transparent / 양면. 기본 Shader Graph 노드로 테이퍼와 알파 마스크를 구성했습니다. VFX 지원을 켜두었습니다.
- `ArcSlash_White.mat`: 흰색 기본 머티리얼.
- `ArcSlash_StaticPreview.prefab`: 메시와 머티리얼을 연결한 정적 프리팹. 씬으로 드래그해 확인할 수 있습니다. 정면은 Z축 방향입니다.

## 조절

| 프로퍼티 | 기본값 | 역할 |
|---|---:|---|
| Width | 0.45 | 머티리얼과 VFX 출력의 현재 폭. 0이면 숨김. Shader Graph 자체 기본값은 0.8 |
| TailPower | 2 | 높일수록 꼬리가 길게 가늘어지고 두께가 앞쪽에 집중됨 |
| HeadTaper | 0.12 | 앞쪽 끝이 좁아지는 구간의 길이 |
| Opacity | 1 | 전체 투명도. VFX 재생 시 페이드 제어에 사용 |
| Color | 흰색 | 기본 색상 |

전체 크기는 프리팹의 Transform Scale로 조절합니다. 기본 각도와 폭은 제작 시작값이며 최종 연출 수치가 아닙니다.

그래프는 UV 분리 → 꼬리 Power와 앞쪽 Smoothstep → 폭 결합 → V 중심과 거리 비교 → 알파 순서입니다. 별도 텍스처나 Custom Function은 사용하지 않습니다.

## 재생 조절

- 업데이트 모드: Delta Time. 매 프레임의 Time.deltaTime으로 시뮬레이션하며 Time.timeScale을 따릅니다.
- Initialize / Set Lifetime: 기본 0.15초.
- Update / Set Angle.Z over Life: -35도에서 +85도로 회전. XY 평면에서 반시계 방향입니다.
- Update / Set Alpha over Life: 수명 10% 지점까지 켜지고, 55%까지 유지한 뒤 0으로 페이드됩니다.
- Get Alpha 노드를 Output의 `_Opacity`에 연결해 Shader Graph 알파에 반영합니다.
- Output의 Width / TailPower / HeadTaper / Color에서 외형을 조절합니다. VFX 출력은 정적 미리보기 머티리얼의 값을 사용하지 않습니다.
- 메시 입자 용량은 1이며, 자동 반복은 없습니다. 수명 안에 다시 트리거하면 여러 원호를 겹치는 용도로는 사용할 수 없습니다.

## 직선 섬광

원호 앞쪽 끝에 접하며 화면을 가로지르는 두 번째 시스템입니다(레퍼런스의 D). 같은 Shader Graph를 `TangentRibbon.asset` 메시에 사용합니다.

- `TangentRibbon.asset`: 원점 중심 로컬 X축 양방향 직선 띠. 길이 16(-8~8), 폭 0.05. U는 -8에서 0, +8에서 1입니다.
- Spawn: Burst 1개, Delay 7/60초(약 0.117초). 원호(수명 0.15초)가 소멸하기 직전입니다.
- Initialize: Lifetime 0.2초. 원호가 사라진 뒤에도 남습니다. Position은 해당 시점 원호 앞쪽 끝(약 180도, 반경 1), Angle.Z는 그 접선 방향(약 270도)으로 고정값입니다. 원호 회전 곡선, 수명, 섬광 Delay를 바꾸면 이 두 값도 다시 계산해야 합니다(앞쪽 끝 각도 = 110 + Angle 곡선 값).
- Initialize Bounds: 20×20×4. 긴 직선이 컬링되지 않도록 넓혔습니다.
- Update: Alpha over Life — 30%까지 1 유지 후 0으로 페이드.
- Output: Width 0.7, TailPower 1, HeadTaper 0.5. 접점 부근이 가장 굵고 양 끝으로 가늘어집니다. 미리보기 화면 범위 안에서는 거의 일정한 두께로 보입니다.

## 점선 잔상

원호가 소멸한 직후 같은 원을 따라 남는 노이즈 점선입니다(레퍼런스의 F). 세 번째 시스템이며, Shader Graph 대신 기본 Output Particle Mesh(Unlit)의 텍스처와 알파 클리핑으로 만들었습니다. 레퍼런스에서는 안쪽 원호(B)의 원에도 생기지만 B가 아직 없으므로 원호(A)의 원에만 있습니다.

- `ArcDashRibbon.asset`: ArcRibbon과 같은 -110~110도 원호. 반경 1, 띠 폭 0.035, 256분할. U=0은 꼬리, U=1은 앞쪽 끝입니다.
- `DashNoise.png`: 1024×4 점선 텍스처. 간격 6~16px, 조각 길이 8~24px로 조각 40개이며, 조각마다 알파값이 0.75~1 사이로 무작위입니다(시드 7). Point 필터, 밉맵 없음, Clamp.
- Spawn: Burst 1개, Delay 0.15초. 원호 수명이 끝나는 시점입니다.
- Initialize: Lifetime 0.3초, Angle.Z 85도(원호 회전 곡선의 끝값). Bounds 10×10×4.
- Update: Alpha over Life — 40%까지 1, 끝에서 0.85. 밝기는 거의 유지합니다.
- Output: 양면(Cull Off), Alpha 블렌드, 알파 클리핑 사용. Alpha Threshold는 Age Over Lifetime → Sample Curve로 연결합니다. 곡선은 40%까지 0.1, 45%에 0.62, 100%에 1입니다. 임계값이 조각 알파값을 넘는 순서대로 조각이 하나씩 끊겨 사라집니다.
- 실제로 보이는 구간은 약 10~24프레임이며, 19~24프레임에 점선이 끊겨 사라집니다.

## 점선 잔상 — 파티클 버전

`ArcSlash_ParticleDash.vfx` / `ArcSlash_ParticleDash.prefab`은 `ArcSlash.vfx`를 복제해 점선 잔상만 파티클 방식으로 바꾼 별도 에셋입니다. 원호와 직선 섬광은 원본과 같습니다. 원본 `ArcSlash.vfx`, `DashNoise.png`, `ArcDashRibbon.asset`은 그대로 남아 있습니다.

조각마다 원호 위 위치를 커스텀 속성으로 저장하고, 매 프레임 그 값으로 위치를 다시 계산합니다. 그래서 조각이 원호를 벗어나지 않고 원호를 따라 무작위로 움직입니다.

- 커스텀 속성: `t`(원호 위 위치 0~1), `angSpeed`(t 기준 초당 이동량).
- Spawn: Burst 120개, Delay 0.15초.
- Initialize: capacity 128, `t` = Random Number(0~1)², `angSpeed` -0.25~0.25 무작위, Lifetime 0.6~0.9초 무작위, size 0.016~0.026 무작위(점 지름). scale은 기본값 1을 그대로 둬서 항상 정원입니다.
- Update:
  1. `angSpeed`에 매 프레임 -0.04~0.04 무작위 값을 더합니다. 속도가 랜덤 워크로 바뀝니다.
  2. `t += angSpeed × DeltaTime`.
  3. Position Shape(Circle, Spawn Mode Custom, 반경 1, Arc 220도, 회전 Z 105도)의 Arc Sequencer에 Saturate(`t`)를 연결합니다. VFX 원호는 +Y에서 시작해 시계 방향으로 진행하므로, 회전 105도로 195도에서 -25도까지, 즉 원호의 마지막 위치를 덮습니다.
  4. Angle.Z = 285 - 220 × Saturate(`t`). 조각을 원호 접선 방향으로 정렬합니다. 점이 원형인 지금은 겉모습에 영향이 없지만, 길쭉한 텍스처로 바꿀 때 쓰도록 남겨 두었습니다.
  5. Alpha over Life: 80%까지 1, 끝에서 0.
- Output: Output Particle Quad, Base Color Map Color And Alpha, Main Texture `DotCircle.png`, Alpha 블렌드.
- `DotCircle.png`: 64×64 흰 원. 가장자리 안티앨리어싱은 약 1px로 또렷하게 끊깁니다. 밉맵 사용, Bilinear, Clamp. VFX 기본 `DefaultParticle`(가장자리가 부드럽게 흐려지는 원) 대신 쓴 이유는, 점이 화면에서 3~4px로 아주 작아서 부드러운 텍스처로는 흐릿해지고, 분석글의 "하드 엣지" 방향에도 맞기 때문입니다.
- 글로우 출력: 같은 Update에 두 번째 Output Particle Quad를 연결했습니다. Main Texture `DotGlow.png`, Additive 블렌드이며, 점 본체 위에 부드러운 빛을 겹칩니다.
  - `DotGlow.png`: 64×64 흰색. 알파 = (1 - 중심거리/반경)². 밉맵 사용, Bilinear, Clamp. VFX 패키지의 `DefaultParticle.tga`는 이 프로젝트에서 고리와 축선이 그려진 텍스처로 렌더되어 쓰지 않았습니다.
  - 출력 블록: size × SineWave(age + 파티클별 고정 Random 0~1, 주파수 8Hz, 최소 1.5, 최대 4.5), alpha × 0.6. 출력 블록이라 점 본체 크기와 시뮬레이션에는 영향이 없습니다.
  - 글로우가 점 지름의 1.5~4.5배 사이를 초당 8번 오갑니다. 위상이 파티클마다 달라 점들이 엇갈려 맥동합니다. 수명이 0.6~0.9초라 점 하나당 약 5~7번 맥동합니다.
- 조각마다 수명이 달라 하나씩 사라지며, 약 68프레임(재생 시작 후 약 1.1초)에 모두 소멸합니다. 직선 섬광(약 0.32초에 소멸)보다 오래 남습니다.
- 이동 폭은 `angSpeed` 초기 범위와 Update의 무작위 가산 범위로 조절합니다. 현재 값에서는 수명 동안 몇 도 정도 움직입니다.
- `t`는 Saturate로 0~1에 고정되므로, 끝에 닿은 조각은 원호 양 끝에 머뭅니다.
- 앞쪽 쏠림: `t`는 Random Number(0~1) → Power → Set t로 뽑습니다. Power의 지수는 Exposed 프로퍼티 `DashFrontBias`(기본 2)에 연결되어 있어 VisualEffect 인스펙터나 `SetFloat("DashFrontBias", k)`로 바꿀 수 있습니다. `ArcSlash_Combined.vfx`도 같은 구성입니다.
  - t=0이 원호 앞쪽 끝(195도)이므로 점이 앞쪽에 몰리고 꼬리 쪽은 듬성듬성해집니다. k=2이면 점의 절반 이상이 앞쪽 약 30% 구간에 있습니다.
  - k=1이면 균등 분포(변경 전과 같음)입니다. 4 이상에서는 앞쪽 끝에 점이 겹쳐 뭉칩니다.
  - 비교 캡처: `Docs/Previews/ArcSlash_PDash_FrontBias_Zoom.png`(변경 전·후), `Docs/Previews/ArcSlash_Combined_DashFrontBias_k1_k2_k4.png`(k=1, 2, 4).

## 지면 충격 (E) — 파티클 버전에만 있음

`ArcSlash_ParticleDash.vfx`에만 추가했습니다. 레퍼런스 9~12번처럼, 직선 섬광이 나오는 순간 땅에 닿는 지점에서 광원이 터지고, 지면을 따라 원형 링이 빠르게 퍼졌다 사라집니다. 링을 따라 작은 입자가 있습니다.

- 충돌 지점: (-1, -1.2, 0). 직선 섬광(x = -1) 아래쪽이며, 원호 원의 바닥(y = -1)보다 조금 아래를 지면으로 봅니다. 세 시스템의 위치가 모두 이 값이므로, 옮길 때는 함께 바꿉니다(링 입자는 Position Shape 원의 Transform 위치).
- **E 광원:** Burst 1, Delay 7/60초(직선 섬광과 같은 프레임), 수명 0.1초.
  - Output Particle Quad, `DotGlow.png`, Additive, Orient Face Camera Plane.
  - 색 (1, 0.65, 0.35) × 2.
  - Alpha 곡선: 1에서 30% 지점까지 0.25로 급감한 뒤 0. Size 0.8 → 1.2.
- **E 링:** Burst 1, Delay 8/60초, 수명 0.38초. 처음 0.08초 동안 퍼지고, 나머지 0.3초 동안 노이즈 디졸브로 사라집니다.
  - Output Particle Mesh, `GroundRing.asset`, `RingNoise.png`, Additive, Cull Off, 알파 클리핑 사용.
  - 색 (1, 0.5, 0.12) × 1.3.
  - Size 곡선 = 링 반경: 0.15 → 1.15를 수명 21%(0.08초) 지점까지 ease-out으로 퍼지고, 끝에서 1.2. Alpha는 21%까지 1, 끝에서 0.85.
  - Alpha Threshold = Sample Curve(Age Over Lifetime): 21%까지 0, 이후 끝에서 0.5까지 직선으로 올라갑니다. 링이 단면 가장자리부터 가늘어지고 노이즈가 약한 구간부터 끊기며 사라집니다.
    - 최대값을 1이 아닌 0.5로 둔 이유: 링 선이 화면에서 2~3px이라 밉맵 때문에 실제 알파 최대값이 0.5 안팎으로 떨어집니다. 1까지 올리면 처음 몇 프레임 만에 거의 다 잘렸습니다.
- **E 링 입자 (방사형 줄무늬):** Burst 320, Delay 8/60초, 수명 0.25~0.41초(= 0.25 + 0.16 × 길이와 같은 Noise 값). 오디오 스펙트럼처럼 원을 따라 방사형 줄무늬가 들쭉날쭉하게 서 있는 모양입니다. 수명이 노이즈를 따르므로 이웃끼리 비슷한 시점에 사라져, 링 전체가 동시에 꺼지지 않고 구역별로 사라집니다.
  - Alpha Threshold = Sample Curve(Age Over Lifetime): 30%까지 0, 이후 끝에서 0.5까지 직선. 줄무늬가 노이즈 모양대로 끊기며 짧아집니다.
  - 색 (1, 0.75, 0.4) × 1.5. 점이었던 이전 버전과 같은 색입니다.
  - size 1. scale.x 0.025(두께. 처음 값 0.008은 Main Camera 기준 1픽셀보다 가늘어 Game 뷰에서 줄무늬가 보이지 않아 링 선 폭(0.03)에 가깝게 올림). scale.y = 길이 = 0.04 + 0.22 × Noise² + 0.6 × Random¹².
    - Noise는 Perlin 1D이며, 좌표는 spawnIndex/320, 주파수 12, 옥타브 2입니다. 이웃 줄무늬끼리 비슷한 길이로 묶입니다.
    - Random¹² 항은 드물게만 커져서 가끔 긴 가시를 만듭니다.
  - Output Particle Quad, `StreakNoise.png`, Additive, 알파 클리핑 사용.
    - 출력 블록에서 velocity = normalize(position - 충돌 지점)으로 바깥 방향을 주고, Orient Along Velocity로 줄무늬를 그 방향으로 세웁니다. 출력 블록이라 시뮬레이션에는 영향이 없습니다.
  - `StreakNoise.png`: 16×128. 길이 방향 끝이 흐려지는 프로파일(지수 0.8) × 폭 방향 가운데가 밝은 프로파일 × 길이 방향 fbm Perlin 노이즈(0.2~1). 밉맵 사용, Bilinear, Clamp. 디졸브 전의 `StreakSoft.png`도 남아 있습니다.
  - 커스텀 속성 `ringT` = spawnIndex × 1/320(같은 간격). 처음에는 particleId를 썼는데, particleId는 Reinit 없이 Play를 다시 보내면 0으로 돌아가지 않아서 두 번째 재생부터 값이 1을 넘어 줄무늬 320개가 한 점에 겹쳤습니다. spawnIndex는 발생 묶음마다 0부터 다시 셉니다. Reinit 없이 Play를 두 번, 세 번 보낸 재생에서도 링 전체에 퍼지는 것을 확인했습니다(`Docs/Previews/ArcSlash_Replay2_MainCam_F11.png`, `ArcSlash_Replay3_MainCam_F11.png`).
  - Update의 Position Shape(Circle, Custom, Arc 360도, 중심 = 충돌 지점, 회전 X 90도로 지면에 눕힘)에서 Arc Sequencer = `ringT`, 반경 = 링과 같은 곡선을 Sample Curve로 샘플한 값. 곡선 입력은 Saturate(age / 0.08)로, 링 수명 기준입니다. 그래서 입자가 링 위에 붙어 함께 퍼지고, 링이 사라진 뒤에는 최대 반경에 잠깐 남습니다.
- `GroundRing.asset`: XZ 평면(y = 0)의 360도 원형 띠. 반경 1, 폭 0.03, 128분할. V는 폭 방향.
- `RingLine.png`: 8×64. 알파 = (1 - |2v - 1|)². 띠 가운데만 밝습니다. 디졸브 전에 쓰던 텍스처입니다.
- `RingNoise.png`: 512×32. U는 링 둘레(원 위에서 샘플해 이음매 없음), V는 단면. 알파 = √(단면 프로파일) × (0.15 + 0.85 × fbm Perlin 노이즈). Repeat, 밉맵 사용.
- 색 배율은 HDR이 아닌 미리보기 렌더에서도 주황빛이 남도록 낮게 잡았습니다. 실제 카메라가 HDR과 Bloom을 쓰면 배율을 올려도 됩니다.

검증: 8·9·11·13프레임을 정면 시점과 충돌 지점을 35도 내려다보는 임시 시점으로 렌더링했습니다(`Docs/Previews/ArcSlash_Impact_F09/F11/F13_Front.png`, `_Top.png`, `_Down.png`). 확인한 내용은 다음과 같습니다.
- 8프레임부터 광원과 링이 나타납니다.
- 11프레임 전후로 링이 최대 크기입니다.
- 링 입자 줄무늬 320개가 링을 따라 바깥 방향으로 서서 함께 퍼집니다. 위에서 수직으로 내려다본 `_Down.png`에서 방사형 줄무늬 링 모양이 가장 잘 보입니다.
- 13프레임에 링이 사라지고 줄무늬만 최대 반경에 잠깐 남습니다.
- 정면 시점에서는 지면 링이 옆에서 보여 줄무늬가 가로선처럼 보입니다.

노이즈 디졸브를 넣은 뒤에는 SampleScene의 Main Camera 시점과, 같은 카메라의 뷰 행렬만 바꿔 충돌 지점을 55도 내려다본 시점으로 렌더링했습니다(`Docs/Previews/ArcSlash_EDissolve_F11/F20/F26_Main.png`, `_Top.png`, `F31_Top.png`). 확인한 내용은 다음과 같습니다.
- 11프레임: 링이 최대 크기입니다.
- 20프레임: 링이 남은 채 줄무늬가 노이즈로 가늘어집니다.
- 26프레임: 링이 노이즈 조각으로 끊기고 줄무늬는 대부분 사라집니다.
- 31프레임: E가 모두 사라집니다.

E 시스템을 추가한 뒤에는 같은 시드라도 점선 잔상 파티클의 무작위 배치가 이전 캡처와 달라집니다. 시스템이 늘면서 시스템별 난수가 바뀐 것으로 보이며, 값과 연결은 그대로입니다.

점선 잔상 파티클 버전은 12·18·22·30프레임을 렌더링해 원호 위치, 접선 정렬, 조각별 소멸, 종료 후 빈 화면을 확인했습니다. 점을 원형으로 바꾼 뒤 14·22프레임을 다시 렌더링했습니다(`ArcSlash_PDash_F14_Dot.png`, `ArcSlash_PDash_F22_Dot.png`. 22프레임에는 120개 중 83개가 남아 있습니다). 확대 렌더(`ArcSlash_PDash_F14_Dot_Zoom.png`)에서 겹치지 않은 점의 픽셀 크기가 가로와 세로 같음(예: 23×23, 26×26)을 확인했습니다. 글로우는 시드를 고정해 12·14·16프레임을 렌더링했고(`ArcSlash_PGlow_F12.png`, `ArcSlash_PGlow_F12/F14/F16_Zoom.png`), 같은 점의 글로우 크기가 프레임마다 커졌다 작아지는 것을 확인했습니다(수명 변경 전 캡처). 수명을 늘린 뒤에는 30·50·62·68프레임을 렌더링했습니다(`ArcSlash_PGlow_F30/F50/F62.png`). 남은 점 수는 각각 120, 85, 8, 0이며, 원호 끝에 몰리는 점은 끝마다 한두 개 정도였습니다. 시드를 고정한 11프레임과 19프레임을 겹친 `ArcSlash_PDash_Motion_F11vsF19.png`는 이전 모양(48개 사각 조각)에서 찍은 것이며, 이동 방식은 같습니다. 이 이미지에서는 같은 조각들이 원호를 따라 이동한 것을 볼 수 있습니다(빨강: 11프레임에만 있음, 초록: 19프레임에만 있음, 흰색: 겹침). 미리보기 씬의 VisualEffect는 원본 `ArcSlash.vfx`를 사용하므로, 이 버전은 프리팹이나 VisualEffect의 에셋을 바꿔서 확인합니다.

Unity 에디터 실제 렌더링으로 정적 형태와 Opacity 0/1을 확인했습니다. 별도 씬에서 VisualEffect.Simulate를 1/60초 단위로 진행하여 회전, 페이드, 이후 빈 화면을 확인했습니다. `Docs/Previews/VFX_02.png`, `VFX_05.png`, `VFX_08.png`, `VFX_13.png`가 검증 캡처입니다. 직선 섬광은 같은 방식으로 7·8·12·16·21프레임을 렌더링해 등장 시점(8프레임), 원호 끝과의 접점, 원호 소멸 후 유지와 페이드, 21프레임의 빈 화면을 확인했습니다(`ArcSlash_Line_F07/F08/F12/F16.png`). 점선 잔상은 12·19·22·25·29프레임을 렌더링해 원호 자리의 점선 생성, 유지, 조각별 소멸, 25프레임 이후 빈 화면을 확인했습니다(`ArcSlash_Dash_F12/F19/F22.png`). 에디터 창이 비활성이면 플레이어 루프가 멈춰 Simulate가 반영되지 않으므로, Simulate 뒤 `EditorApplication.QueuePlayerLoopUpdate()`를 호출하고 다음 호출에서 캡처합니다. Play Mode는 실행하지 않았습니다.

## 궤적이 일그러지는 베기 (SlashDistortion)

nattuhan의 「軌跡が歪む斬撃エフェクトをつくる」 튜토리얼을 따라 만든 별도 이펙트입니다. 뒤쪽 화면을 Scene Color로 일그러뜨리고, 그 위에 발광 띠를 얹습니다. 영상의 Particle System 대신 VFX Graph로 재생합니다.

- `SlashMesh.fbx`: 반원 띠. 바깥 반지름 1, 안쪽 0.6. XZ 평면(법선 +Y), 호는 +Z 쪽으로 볼록합니다. U는 호 길이 방향(+X 끝 0, −X 끝 1), V는 폭 방향(바깥 0, 안쪽 1).
- `Noise Texture.png`: 1024×1024 가로 결 노이즈. sRGB 꺼짐, Repeat. 좌우만 이어지고 위아래 이음새는 남아 있습니다. 원본은 `Docs/References/NoiseTexture_original.png`.
- `SlashShader.shadergraph`: URP Unlit / Transparent / 양면 / 그림자 없음 / VFX 지원. 그래프 그룹 순서는 UV Scroll → Normal Distortion → UV Mask → Emission → Scene Color입니다.
- `SlashShader.mat`: 정적 확인용 머티리얼. VFX 재생에는 쓰이지 않습니다.
- `SlashDistortion.vfx`: `ArcSlash.vfx`의 첫 시스템을 복제해 만들었습니다. Single Burst 1개, Lifetime 0.45초, Angle.Y 90 → −150도(처음 빠르고 끝에 멈춤), Alpha over Life → `_Fade`.

| 프로퍼티 | 기본값 | 역할 |
|---|---:|---|
| ScrollSpeed | 1 | 노이즈 UV의 가로 스크롤 속도 |
| Distortion | 0.05 | 배경 일그러짐 세기(0~0.1) |
| SlashWidth | 1 | 발광 띠 길이. UV (1, 0) 모서리에서의 거리 기준(0~1.5) |
| EmissionColor | HDR (1, 2.4, 4) | 발광 색. Bloom 임계값 1을 넘게 둡니다 |
| Fade | 1 | 전체 투명도. VFX에서 파티클 알파로 제어 |

- UV 마스크는 U와 V 양 끝을 Smoothstep으로 깎습니다. 발광이 있는 U=1 쪽만 1 → 0.9로 좁혔습니다. 마스크는 디스토션 세기와 Alpha에 함께 곱합니다.
- VFX 출력의 셰이더를 바꾸면 이전 셰이더의 슬롯 값이 순서대로 옮겨 들어갑니다. 셰이더를 다시 바꿀 때는 Output 입력값을 셰이더 기본값으로 되돌렸는지 확인합니다.
- SampleScene의 `SlashDistortionTest`에 VFX(`SlashVFX`, X −70도로 기울임), 비활성 정적 메시, 확인용 큐브 3개(`Backdrop`)가 있습니다.

## ArcSlash_Combined — 인스펙터 프로퍼티

`ArcSlash_Combined.vfx`의 Exposed 프로퍼티입니다. VisualEffect 인스펙터의 Properties 또는 `SetVector4` / `SetFloat`으로 바꿉니다. 색은 모두 HDR이며, 기본값은 노출하기 전 값과 같습니다.

| 프로퍼티 | 기본값 | 대상 |
|---|---:|---|
| CreamColor / GoldColor | HDR (2.5, 2.33, 1.95) / (3, 1.98, 0.6) | 베기 메시(SlashShader)의 두 색 |
| ImpactPosition | (-1, -1.2, 0) | 지면 충격 위치 |
| DashFrontBias | 2 | 점선 `t` = Random^k의 지수 k. 1이면 균등, 높을수록 원호 앞쪽에 몰림 |
| ArcColor | (1, 1, 1) | 원호(A) Output `_Color` |
| FlashColor | (1, 1, 1) | 직선 섬광(D) Output `_Color` |
| DashColor | (1, 1, 1) | 점선(F) Initialize의 Set Color. 점 본체와 글로우에 함께 적용 |
| ImpactLightColor | HDR (2, 1.3, 0.7) | E 광원 Set Color |
| ImpactRingColor | HDR (1.3, 0.65, 0.16) | E 링 Set Color |
| ImpactStreakColor | HDR (1.5, 1.13, 0.6) | E 링 입자 Set Color |
| DashCount | 120 (0~512) | 점선 Single Burst 개수. 점선 시스템 capacity를 128에서 512로 올림 |
| DashMoveSpeed | 1 (0~5) | 점선 이동 배율. Update의 `t += angSpeed × DeltaTime × DashMoveSpeed`. 0이면 정지 |
| FlashTrailColor | (1, 1, 1) | 직선 잔상(D 잔상) Initialize의 Set Color. 점 본체와 글로우에 함께 적용 |
| FlashEndY | -0.8 | 직선(D)의 하단 클리핑, 직선 잔상의 생성 하한과 충돌면 Y를 함께 조절. ImpactPosition과 독립 |

- 색 프로퍼티는 Color 타입입니다. Set Color 블록(RGB)에 연결된 것은 알파를 쓰지 않습니다.
- `DashCount`를 512보다 크게 주면 capacity에 막혀 512개까지만 생성됩니다.
- 기본값과 변경값 비교 캡처: `Docs/Previews/ArcSlash_Combined_ExposedProps_Default_vs_Modified.png`

## 직선 잔상 (D 잔상) — ArcSlash_Combined에만 있음

직선 섬광(D)이 사라진 뒤 그 직선을 따라 남는 점 입자입니다. 점선 잔상(F)과 같은 점·글로우 텍스처를 쓰고, 생긴 자리에서 아래로 떨어지며 사라집니다. 그래프에서는 다른 시스템 오른쪽(x = 10000)에 있습니다.

- Spawn(`D 잔상 시작 (19/60초)`): Single Burst 120개, Delay 19/60초. D의 등장(7/60초) + 수명(0.2초) = 소멸 시점입니다.
- Initialize: capacity 128, Bounds 중심 (-1, -1.5, 0) 크기 (2, 9, 2).
  - Position: (-1, FlashEndY, 0) ~ (-1, 2.8, 0) 균등 무작위. D가 지나는 x = -1 직선 위에 생성됩니다.
  - Velocity: (-0.03, -0.9, 0) ~ (0.03, -0.3, 0) 균등 무작위. 아래 방향이고 좌우로 아주 조금 흩어집니다.
  - Lifetime 0.6~0.9초, size 0.005~0.01(F와 같음).
  - Set Color: 노출 프로퍼티 `FlashTrailColor`(기본 흰색)에 연결.
- Update: Gravity (0, -1.5, 0)로 떨어지는 속도가 점점 빨라집니다. Alpha over Life — 80%까지 1, 끝에서 0.
  - 수평 충돌면의 높이도 FlashEndY에 연결합니다. Bounce 0, Friction 1로 해당 높이에서 멈춘 뒤 수명에 따라 사라집니다.
- Output: 점 본체(Quad, `DotCircle.png`, Alpha 블렌드)와 글로우(Quad, `DotGlow.png`, Additive, size × 3, alpha × 0.6). F의 글로우와 달리 맥동은 없습니다.
- 낙하 거리는 Velocity 범위와 Gravity로, 남는 시간은 Lifetime으로 조절합니다. 생성 범위를 바꿀 때는 D의 위치(Initialize의 Position x = -1)와 함께 맞춥니다.

검증: 22·45프레임에서 시스템별 입자 수(D 잔상 120개)와 직선 위 배치를 확인했습니다. 시드를 고정한 30프레임과 36프레임의 직선 위 밝기 분포를 비교해, 점 무늬가 6프레임 동안 화면 아래로 12px 이동한 것을 확인했습니다(당시 Main Camera (5, 4, -5), 회전 (30, 315, 0)).
