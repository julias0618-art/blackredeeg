# 내 씬(Prefab/SampleScene) — Inspector & XR 연동 체크리스트

---

## 0. 다음 할 일 (권장 순서)

지금 **2D Game 뷰**로만 확인 중이면, **XR(Quest)**에서 쓰던 느낌이랑 다르게 보일 수 있어요. 아래 순서로 진행하는 걸 추천합니다.

| 순서 | 할 일 | 참고 |
|------|--------|------|
| ① | **데스크톱 2D에서 동작 확인** | Play → 화살표/키보드, Move/Zoom, LSL 마커 전송 등 |
| ② | **씬에 XR Origin 추가** | GameObject → XR → **XR Origin (VR)** (§0-2) |
| ③ | **XR용 설정 전환** | Main Camera 끄기, ZoomTaskRunner → XR Camera, Zoom By Target Scale, CameraFollow 끄기 등 (아래 §3 참고) |
| ④ | **에디터에서 XR 시뮬레이터로 확인** | Game 뷰 해상도·디바이스 시뮬레이터 켜서 3D 뷰에 가깝게 테스트 |
| ⑤ | **Quest 빌드** | Build Settings → Android, Quest로 빌드 후 기기에서 재확인 |

**2D 뷰** = 평면 한 카메라. **XR** = 양눈 스테레오 + 머리 움직임. 해상도·UI 크기·텍스트 비율이 달라서 “다르게” 보이는 건 정상에 가깝고, **해상도·Canvas 설정**을 맞추면 줄어듭니다 (§ “2D vs XR · 텍스트” 참고).

---

### 0-1. 데스크톱 2D 동작 확인 — 어떻게 하나요?

**준비:**  
1. **Assets/Prefab/SampleScene** 열기  
2. **Game** 뷰가 보이게 해 두기 (상단 **Game** 탭 클릭)  
3. **Play** 버튼(▶) 누르기  

**주의:** 키보드 입력은 **Game 뷰가 포커스**되어 있을 때만 됩니다. Play 후 **Game 뷰 한 번 클릭**해서 포커스 주기.

| 키 | 하는 일 |
|----|--------|
| **← → ↑ ↓** | Move 큐 선택 (왼/오른/위/아래). 화면에 화살표(←→↑↓) 표시 |
| **I** | Zoom In 큐 선택. "ZOOM +" 표시 |
| **O** | Zoom Out 큐 선택. "ZOOM -" 표시 |
| **Space** | **(1)** 아무것도 안 골랐을 때: 랜덤 Move 또는 Zoom 선택 후 큐 표시<br>**(2)** 큐 선택한 뒤: **실행** (공 움직임 / 줌) + **LSL 마커 전송**<br>**(3)** 실행이 끝난 뒤: 센터로 **복귀** |

**확인할 것:**  
- Canvas에 ←→↑↓ / ZOOM +·ZOOM - 나오는지  
- 검은 공이 움직이거나, 줌 인/아웃 되는지  
- **Console**에 `[SELECT]`, `[EXECUTE]`, `[RETURN]`, `[LSL] …` 로그 나오는지  

**LSL 마커:** `LSLMarkerSender`의 **Enable LSL**이 켜져 있으면, **Space로 실행할 때만** 마커가 전송됩니다.

---

### 0-2. XR Origin — 어디서 찾나요?

**위치:** Unity 상단 메뉴  
**GameObject → XR → XR Origin (VR)**

- **XR Origin (VR)** = PC VR / Quest 등 **헤드셋용**  
- **XR Origin (Mobile AR)** = 휴대폰 AR용. Quest는 **(VR)** 사용.

**추가 후:**  
- Hierarchy에 **XR Origin (VR)** 생김  
- 자식에 **Main Camera** (XR용 카메라) 있음 → **Tag = MainCamera**  
- 기존 **Main Camera**가 이미 있으면, XR 쓸 때는 **기존 Main Camera 비활성화**하고, **ZoomTaskRunner → Main Camera**를 **XR Origin 안의 Main Camera**로 바꾸기 (§3 참고).

---

## 1. Floor 흰색 복구 ✅
- **Floor** 오브젝트 → **Mesh Renderer** → **Materials** 을 **Mat_white** 로 설정했습니다.
- 붉은색이었던 건 이전에 다른 머티리얼(데모용 등)이 할당돼 있었기 때문입니다.

---

## 2. Inspector에서 꼭 확인할 참조 (데스크톱/XR 공통)

씬에 **StimulusController** 가 붙은 오브젝트를 선택한 뒤, 아래 컴포넌트들도 함께 확인하세요.

### 2.1 StimulusController
| 필드 | Hierarchy에서 드래그할 오브젝트 |
|------|-------------------------------|
| **Cue UI** | **Canvas** (CuePresenter 붙어 있음) |
| **Move Runner** | **Blackball** (MoveTaskRunner 붙어 있음) |
| **Zoom Runner** | **Main Camera** (ZoomTaskRunner 붙어 있음) |
| **Marker Sender** | **GameManager** (LSLMarkerSender 붙어 있음) |

**드래그가 안 될 때:**

1. **Object Picker(동그라미) 사용**  
   Ref 필드 오른쪽 **◎** 아이콘 클릭 → 나온 창에서 **해당 타입** 오브젝트 선택.  
   - Cue UI → `CuePresenter` 검색 → **Canvas** 선택  
   - Move Runner → `MoveTaskRunner` 검색 → **Blackball** 선택  
   - Zoom Runner → `ZoomTaskRunner` 검색 → **Main Camera** 선택  
   - Marker Sender → `LSLMarkerSender` 검색 → **GameManager** 선택  

2. **Play 모드인지 확인**  
   ▶ 재생 중이면 Ref 수정이 안 될 수 있음. **Stop** 후에 할당.

3. **Inspector 잠금**  
   StimulusController 있는 **Inspector** 탭에 자물쇠 잠금이 켜져 있으면 끄기.

4. **Game 뷰 말고 Hierarchy에서 드래그**  
   **Hierarchy**에서 위 오브젝트를 끌어다가 **Inspector**의 해당 Ref 칸에 놓기.  
   (Project에서 에셋 드래그하면 타입이 안 맞아서 안 됨.)

5. **StimulusController가 붙은 오브젝트 선택**  
   **GameManager** 선택 → Inspector에서 **StimulusController** 컴포넌트의 Refs 칸에 넣는지 확인.

6. **드래그 여전히 안 되면 → 메뉴로 한 번에 연결**  
   상단 메뉴 **Tools → Assign StimulusController Refs** 실행.  
   Hierarchy에 `GameManager`, `Canvas`, `Blackball`, `Main Camera`가 있으면 Ref를 자동 할당합니다.  
   끝나면 **씬 저장(Ctrl+S)** 해 두세요.

### 2.2 MoveTaskRunner
| 필드 | 연결할 대상 |
|------|-------------|
| **Black Ball** | 움직이는 검은 공(Sphere) **Transform** |
| **Target Ball** | Move 시 나타나는 목표 공(Sphere) **Transform** |

### 2.3 ZoomTaskRunner
| 필드 | 연결할 대상 |
|------|-------------|
| **Main Camera** | 메인 카메라 (데스크톱: Main Camera / XR: XR Camera) |
| **Cam Follow** | 그 카메라에 붙은 **CameraFollow** 컴포넌트 |
| **Target Ball** | Move 쪽과 같은 **Target Ball** (Zoom 시 scale 쓰면) |

- **Zoom By FOV** : 데스크톱에서 카메라 FOV로 줌할 때 켜기.
- **Zoom By Target Scale** : XR(Quest)에서는 FOV 조절이 어려우니 **이걸 켜고** FOV/도리 줌은 끄는 걸 권장.

### 2.4 CameraFollow (Main Camera 또는 XR Camera에 부착)
| 필드 | 연결할 대상 |
|------|-------------|
| **Target** | 검은 공(**Black Ball**) **Transform** |

- **Follow X/Z** : 평면 이동만 따라가면 체크 유지.

### 2.5 BillboardNoRoll (캐릭터/UI 스프라이트 등에 부착)
| 필드 | 연결할 대상 |
|------|-------------|
| **Cam** | (선택) 메인 카메라. 비워두면 `Camera.main` 자동 사용. |

### 2.6 CuePresenter (Canvas 자식 오브젝트 등)
| 필드 | 연결할 대상 |
|------|-------------|
| **Cue Text** | Move 큐(←→↑↓)用 TMP_Text |
| **Zoom Text** | Zoom 큐(ZOOM +/‑)용 TMP_Text |

### 2.7 LSLMarkerSender
- **Enable LSL**, **LSL Stream Name** 등 설정만 확인하면 됨. 씬 내 다른 오브젝트 참조는 없음.

---

## 3. XR(Quest) 연동 시 바꿔야 할 것

### 3.1 씬에 XR 추가
1. **Window → Package Manager** 에서 **XR Interaction Toolkit** 사용 중인지 확인.
2. **GameObject → XR → XR Origin (XR Rig)** 로 XR Origin 생성.
3. 자식에 **XR Camera** (또는 **Center Eye Camera**) 가 있고, **Tag = MainCamera** 인지 확인.

### 3.2 “메인 카메라” 역할 바꾸기
- **기존 Main Camera** (단독 카메라):
  - Quest 빌드에서는 **비활성화(Disable)** 하거나 삭제.
- **XR Camera**:
  - **Tag = MainCamera** 유지.
  - **ZoomTaskRunner → Main Camera** 를 **XR Camera** 로 연결.
  - **CameraFollow** 를 XR Camera에 둘지 말지는 설계에 따라 (아래 참고).

### 3.3 CameraFollow / Zoom (XR에서 중요)
- **CameraFollow** 는 “카메라가 검은 공을 따라간다”는 동작인데, XR에서는 **카메라 = 사용자 머리**이므로, **공이 카메라를 따라가게 하면 안 됩니다**.
- **권장**:
  - **XR 사용 시**: **CameraFollow** 는 **비활성화** 하거나, **ZoomTaskRunner** 의 **Cam Follow** 를 비워 두기.
  - 대신 **MoveTaskRunner** 로 검은 공만 이동시키고, 사용자는 고정된 위치에서 머리로 보도록 구성.

### 3.4 ZoomTaskRunner (XR)
- **Zoom By FOV** / **Zoom By Camera Dolly** 는 **끄기** (XR 카메라 FOV/위치 제어 비권장).
- **Zoom By Target Scale** **켜기** → **Target Ball** 에 scale 줌용 공 연결.
- **Main Camera** = **XR Camera** 로 지정.

### 3.5 Canvas (UI)
- **Render Mode = Screen Space - Overlay** 이면 **Event Camera** 없어도 동작.
- **Screen Space - Camera** 로 쓰면 **Event Camera** 에 **XR Camera** 할당.
- **World Space** (퀘스트 안 3D 공간 UI)면 **Event Camera = XR Camera** 로 두고, Canvas를 XR Origin 자식으로 두는 구성도 가능.

### 3.6 Event System
- **XR Interaction Toolkit** 쓰면 **XR UI Input Module** 등 사용할 수 있음.
- **Event System** 에서 **XR Tracking Origin** (또는 XR Origin **Transform**) 이 필요하면 **Inspector** 에서 연결.

### 3.7 BillboardNoRoll
- **Cam** 비워두면 `Camera.main` → XR일 때 **XR Camera** 자동 사용.  
  큐/캐릭터가 항상 사용자 쪽을 보게 할 때 그대로 두면 됨.

---

## 4. 2D Game 뷰 vs XR · 텍스트/UI가 다르게 보일 때

**2D 게임 화면**으로만 보면 **3D XR(Quest)**에서 쓰던 것과 레이아웃·텍스트 크기가 달라 보일 수 있어요. 원인과 보정 방법만 정리합니다.

### 4.1 왜 다르게 보이나?
- **2D Game 뷰**: 한 개 카메라, 창 해상도(또는 독립 해상도)에 맞춰 UI·텍스트 스케일.
- **XR(Quest)**: 스테레오 렌더, 기기 해상도·비율이 다름. Canvas **Reference Resolution**·**Match**에 따라 스케일이 달라짐.
- **에디터 Game 뷰 해상도**를 Quest랑 다르게 두면 2D에서 보는 UI 크기와 Quest에서 보는 게 어긋남.

### 4.2 Canvas / 텍스트 보정 (공통)
- **Canvas** 선택 → **Canvas Scaler**  
  - **Reference Resolution**: 지금 800×600. Quest는 보통 세로 비율이 다르므로, **Match Width Or Height**로 **높이 비율**을 맞추는 걸 많이 씀 (예: **Match = 0.5** 전후).
  - **Scale Factor**: 2D에서 너무 크거나 작으면 1보다 살짝 올리거나 내려서 조정.
- **TMP (Cue / Zoom 텍스트)**  
  - **Font Size**가 2D에서만 안 맞으면: 해당 **TextMeshPro - Text**에서 **Font Size** 조절.  
  - **Auto Size** 쓰면 해상도 바뀔 때마다 크기 변할 수 있음. **끄고** 고정 크기 쓰는 게 비교하기 쉬움.
- **RectTransform**  
  - Cue Text / Zoom Text의 **Anchor**·**Pivot**·**Pos Y** 등으로 화면 안 위치만 조정해도 “어긋난다” 느낌이 많이 줄어듦.

### 4.3 2D에서 “오류처럼” 보이는 경우
- **TMP 글자가 깨지거나 Missing**  
  → **Window → Text Mesh Pro → Import TMP Essential Resources** 실행 여부 확인.  
  → 해당 텍스트 오브젝트 **Font Asset**이 **None**이면 TMP 기본 폰트(또는 사용 폰트) 다시 할당.
- **캔버스가 안 보이거나 레이아웃이 이상함**  
  → **Canvas** **Render Mode**: **Screen Space - Overlay**면 카메라 없이 동작. **Screen Space - Camera**면 **Event Camera**에 메인(또는 XR) 카메라 연결했는지 확인.
- **Char_Up ~ Right 같은 스프라이트가 안 보임**  
  → **Sprite**가 **character** 등 올바른 에셋 연결됐는지, **BillboardNoRoll**의 **Cam**이 비어 있거나 `Camera.main` 쓰는지 확인.

### 4.4 XR에서 테스트할 때
- **에디터**: **XR Device Simulator** 등으로 **Game 뷰**를 3D에 가깝게 본 뒤, **해상도 프리셋**을 **Quest 계열**로 맞추고 위 Canvas/텍스트 설정으로 조정.
- **실기기**: **Android(Quest)** 빌드해서 **실제 해상도**에서 Cue / Zoom 텍스트 크기·위치 한 번 더 확인하는 걸 권장.

---

## 5. 요약 표

| 구분 | 데스크톱 | XR(Quest) |
|------|----------|-----------|
| **메인 카메라** | Main Camera | XR Camera (Tag: MainCamera) |
| **CameraFollow** | 사용 (Target = Black Ball) | 비활성화 또는 Cam Follow 미연결 권장 |
| **ZoomTaskRunner** | Zoom By FOV 등 사용 | **Zoom By Target Scale** 사용, FOV/도리 끄기 |
| **Canvas Event Camera** | Overlay면 불필요 | Camera 모드면 XR Camera 연결 |
| **Floor** | Mat_white ✅ | Mat_white ✅ |

---

## 6. 빠르게 확인하는 순서

1. **Floor** → Mesh Renderer → Material **Mat_white** 인지 확인.
2. **StimulusController** → Cue UI, Move Runner, Zoom Runner, Marker Sender 연결 확인.
3. **MoveTaskRunner** → Black Ball, Target Ball 연결 확인.
4. **ZoomTaskRunner** → Main Camera, Cam Follow, Target Ball 연결 확인.
5. **CameraFollow** → Target = Black Ball 확인.
6. XR 씬이면 **XR Origin + XR Camera** 넣고, 위 “3. XR 연동 시 바꿔야 할 것” 대로 카메라·Zoom·Canvas 정리.

이대로 맞추면 “원래 프로그램”처럼 흰색 Floor 유지되고, XR 연동 시에도 Inspector에서 바꿀 포인트를 따라 할 수 있습니다.
