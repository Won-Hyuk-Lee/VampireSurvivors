# Vampire Survivors Clone - Task List

> **Unity 버전**: 6000.0.63f1
> **개발 언어**: C#
> **최종 업데이트**: 2025-12-25

---

## Phase 1: Utilities & Core Systems

### 0. Utilities ✅ 완료
> 경로: `Assets/Scripts/Utilities/`

| 파일 | 설명 | 상태 |
|------|------|------|
| `Singleton.cs` | 제네릭 싱글톤 베이스 클래스 (DontDestroyOnLoad, 스레드 안전) | ✅ |
| `Constants.cs` | 태그, 레이어, 씬 이름, 애니메이션 파라미터 상수 | ✅ |
| `ExtensionMethods.cs` | Vector2/3, Transform, GameObject 확장 메서드 | ✅ |
| `EventManager.cs` | 이벤트 발행/구독 시스템 + GameEvents 상수 | ✅ |
| `Timer.cs` | 코루틴 없는 타이머 + TimerManager 싱글톤 | ✅ |
| `GameManager.cs` | 게임 상태 관리 (Ready/Playing/Paused/GameOver), 경과 시간 | ✅ |
| `AudioManager.cs` | BGM/SFX 재생, 볼륨 조절, 페이드, SFX 풀링 | ✅ |
| `SaveManager.cs` | JSON 파일 저장 + PlayerPrefs 백업, 해금 시스템 | ✅ |

### 1. Data Pipeline (Editor Tools) ✅ 완료
> 경로: `Assets/Scripts/Editor/`

| 파일 | 설명 | 상태 |
|------|------|------|
| `JsonToScriptableObject.cs` | JSON → SO 변환, 배열 JSON → 다중 SO | ✅ |
| `CsvToScriptableObject.cs` | CSV → SO 변환, SO 클래스 자동 생성 기능 | ✅ |

**사용법**: Unity 에디터 메뉴 `Tools > Data Pipeline`

---

### 2. Object Pooling (Generic) ✅ 완료
> 경로: `Assets/Scripts/Core/`

| 파일 | 설명 | 상태 |
|------|------|------|
| `IPoolable.cs` | 풀링 오브젝트 인터페이스 (OnSpawn, OnDespawn) | ✅ |
| `ObjectPool.cs` | 제네릭 오브젝트 풀 클래스 (Stack 기반, 자동 확장) | ✅ |
| `PoolableObject.cs` | IPoolable 구현 기본 MonoBehaviour 클래스 | ✅ |
| `PoolManager.cs` | 여러 풀을 중앙 관리하는 싱글톤 | ✅ |

### 3. Character System
> 경로: `Assets/Scripts/Character/`, `Assets/Scripts/Data/`

- [ ] `CharacterDataSO` ScriptableObject 정의 (체력, 이동속도, 공격력 등)
- [ ] `CharacterBase` 추상 클래스 구현 (공통 로직)
- [ ] `Player` 클래스 구현 (입력 처리, 이동)
- [ ] `PlayerAttack` 클래스 구현 (기본 공격 로직)
- [ ] `Projectile` 클래스 구현 (투사체, IPoolable 적용)

### 4. Monster System
> 경로: `Assets/Scripts/Monster/`, `Assets/Scripts/Data/`

- [ ] `MonsterDataSO` ScriptableObject 정의 (체력, 이동속도, 공격력, 경험치 등)
- [ ] `MonsterBase` 추상 클래스 구현 (플레이어 추적, 피격, 사망)
- [ ] 기본 몬스터 클래스 구현 (IPoolable 적용)

### 5. Monster Spawner
> 경로: `Assets/Scripts/Spawner/`

- [ ] `SpawnDataSO` ScriptableObject 정의 (스폰 간격, 몬스터 종류, 난이도 곡선)
- [ ] `MonsterSpawner` 클래스 구현 (화면 밖 랜덤 위치 스폰)

### 6. Camera
> 경로: `Assets/Scripts/Camera/`

- [ ] `CameraFollow` 클래스 구현 (플레이어 추적, 스무스 이동)

---

## Phase 2: Integration & Testing

- [ ] 씬에 Player, Spawner, Camera 배치
- [ ] 오브젝트 풀 초기화 및 연동 확인
- [ ] 몬스터 스폰 및 추적 테스트
- [ ] 투사체 발사 및 충돌 테스트
- [ ] JSON/CSV 데이터 변환 테스트

---

## 폴더 구조 (예정)

```
Assets/
├── Scripts/
│   ├── Utilities/       ✅ 완료
│   ├── Editor/          ✅ 완료
│   ├── Core/            ✅ 완료 (Object Pooling)
│   ├── Character/       (Player, Attack)
│   ├── Monster/         (MonsterBase, Spawner)
│   ├── Camera/          (CameraFollow)
│   └── Data/            (ScriptableObjects 클래스)
├── ScriptableObjects/   (데이터 에셋)
├── Prefabs/
├── Scenes/
└── ...
```
