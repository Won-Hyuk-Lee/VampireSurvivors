# Phase 2: Integration & Testing - 직접 해야 할 일

> Unity 에디터에서 직접 수행해야 하는 작업 목록입니다.

---

## 1. 레이어 및 태그 설정

### 레이어 추가 (Edit > Project Settings > Tags and Layers)
- [ ] `Player` 레이어 추가
- [ ] `Enemy` 레이어 추가
- [ ] `Projectile` 레이어 추가
- [ ] `Pickup` 레이어 추가 (경험치 오브 등)

### 레이어 충돌 설정 (Edit > Project Settings > Physics 2D)
- [ ] `Player` ↔ `Enemy` : 충돌 O
- [ ] `Projectile` ↔ `Enemy` : 충돌 O
- [ ] `Projectile` ↔ `Player` : 충돌 X
- [ ] `Enemy` ↔ `Enemy` : 충돌 X (몬스터끼리 밀치지 않음)

---

## 2. 플레이어 프리팹 생성

### 2.1 GameObject 생성
1. Hierarchy에서 **Create Empty** → 이름: `Player`
2. 자식으로 **Sprite** 추가 (임시 스프라이트 할당)

### 2.2 컴포넌트 추가
- [ ] `Rigidbody2D` 추가
  - Body Type: `Dynamic`
  - Gravity Scale: `0`
  - Constraints > Freeze Rotation Z: ✅
- [ ] `CircleCollider2D` 또는 `BoxCollider2D` 추가
- [ ] `Player` 스크립트 추가
- [ ] `PlayerAttack` 스크립트 추가

### 2.3 CharacterData SO 생성
1. Project 창에서 **Create > VampireSurvivors > Character Data**
2. 이름: `DefaultPlayerData`
3. 값 설정:
   - Max Health: `100`
   - Move Speed: `5`
   - Attack Power: `10`
   - Attack Speed: `1`
   - Critical Chance: `0.05`
   - Critical Multiplier: `1.5`

### 2.4 Player 설정
- [ ] Player 컴포넌트의 `Character Data`에 `DefaultPlayerData` 할당
- [ ] Layer를 `Player`로 설정
- [ ] 프리팹으로 저장: `Assets/Prefabs/Player.prefab`

---

## 3. 투사체 프리팹 생성

### 3.1 GameObject 생성
1. **Create Empty** → 이름: `Projectile`
2. 자식으로 **Sprite** 추가 (작은 원 또는 화살 스프라이트)

### 3.2 컴포넌트 추가
- [ ] `Rigidbody2D` 추가
  - Body Type: `Dynamic`
  - Gravity Scale: `0`
- [ ] `CircleCollider2D` 추가
  - Is Trigger: ✅
- [ ] `Projectile` 스크립트 추가
- [ ] `PoolableObject` 컴포넌트는 Projectile이 상속받으므로 자동 포함

### 3.3 Projectile 설정
- [ ] Speed: `15`
- [ ] Max Lifetime: `5`
- [ ] Pierce Count: `0` (관통 없음)
- [ ] Enemy Layer Mask: `Enemy` 선택
- [ ] Layer를 `Projectile`로 설정
- [ ] 프리팹으로 저장: `Assets/Prefabs/Projectile.prefab`

### 3.4 PlayerAttack 연결
- [ ] Player의 `PlayerAttack` 컴포넌트에서:
  - Projectile Prefab: `Projectile` 프리팹 할당
  - Projectile Pool Key: `"PlayerProjectile"`
  - Enemy Layer Mask: `Enemy` 선택

---

## 4. 몬스터 프리팹 생성

### 4.1 GameObject 생성
1. **Create Empty** → 이름: `BasicMonster`
2. 자식으로 **Sprite** 추가

### 4.2 컴포넌트 추가
- [ ] `Rigidbody2D` 추가
  - Body Type: `Dynamic`
  - Gravity Scale: `0`
  - Constraints > Freeze Rotation Z: ✅
- [ ] `CircleCollider2D` 추가
- [ ] `BasicMonster` 스크립트 추가

### 4.3 MonsterData SO 생성
1. **Create > VampireSurvivors > Monster Data**
2. 이름: `BasicMonsterData`
3. 값 설정:
   - Monster Id: `"basic_monster"`
   - Display Name: `"기본 몬스터"`
   - Max Health: `20`
   - Move Speed: `2`
   - Contact Damage: `10`
   - Exp Reward: `10`
   - Prefab: 아래에서 생성한 프리팹 할당

### 4.4 BasicMonster 설정
- [ ] Monster Data: `BasicMonsterData` 할당
- [ ] Player Layer Mask: `Player` 선택
- [ ] Layer를 `Enemy`로 설정
- [ ] 프리팹으로 저장: `Assets/Prefabs/BasicMonster.prefab`
- [ ] `BasicMonsterData`의 Prefab 필드에 이 프리팹 할당

---

## 5. 스포너 설정

### 5.1 SpawnData SO 생성
1. **Create > VampireSurvivors > Spawn Data**
2. 이름: `DefaultSpawnData`
3. 값 설정:
   - Base Spawn Interval: `2`
   - Min Spawn Interval: `0.5`
   - Min Spawn Distance: `10`
   - Max Spawn Distance: `15`
   - Max Monster Count: `50`

### 5.2 Spawner GameObject 생성
1. Hierarchy에서 **Create Empty** → 이름: `MonsterSpawner`
2. `MonsterSpawner` 스크립트 추가
3. 설정:
   - Spawn Data: `DefaultSpawnData` 할당
   - Default Monster Data: `BasicMonsterData` 할당
   - Default Monster Pool Key: `"BasicMonster"`

---

## 6. 카메라 설정

### 6.1 Main Camera 설정
1. Main Camera 선택
2. `CameraFollow` 스크립트 추가
3. 설정:
   - Auto Find Player: ✅
   - Smooth Time: `0.15`
   - Offset: `(0, 0, -10)`

### 6.2 카메라 기본 설정
- [ ] Projection: `Orthographic`
- [ ] Size: `5` ~ `8` (취향에 따라)
- [ ] Background Color: 원하는 색상

---

## 7. 매니저 오브젝트 생성

### 7.1 GameManager
1. **Create Empty** → 이름: `GameManager`
2. `GameManager` 스크립트 추가 (이미 싱글톤이므로 자동 생성될 수 있음)

### 7.2 PoolManager
1. **Create Empty** → 이름: `PoolManager`
2. `PoolManager` 스크립트 추가
3. Pool Configs 배열에 추가:
   - Key: `"PlayerProjectile"`, Prefab: `Projectile`, Initial Size: `20`
   - Key: `"BasicMonster"`, Prefab: `BasicMonster`, Initial Size: `30`

---

## 8. 씬 구성

### 8.1 InGame 씬 구성
```
Hierarchy 구조:
├── GameManager
├── PoolManager
├── MonsterSpawner
├── Player (프리팹 인스턴스)
├── Main Camera (CameraFollow 포함)
└── (선택) Ground/Background 스프라이트
```

### 8.2 씬 저장
- [ ] `Assets/Scenes/InGame.unity` 저장
- [ ] Build Settings에 씬 추가

---

## 9. 테스트 체크리스트

### 9.1 기본 동작 테스트
- [ ] Play 버튼 클릭
- [ ] WASD로 플레이어 이동 확인
- [ ] 카메라가 플레이어를 따라가는지 확인

### 9.2 몬스터 스폰 테스트
- [ ] 몬스터가 화면 밖에서 스폰되는지 확인
- [ ] 몬스터가 플레이어를 추적하는지 확인
- [ ] 시간이 지나면 더 많은 몬스터가 스폰되는지 확인

### 9.3 전투 테스트
- [ ] 투사체가 자동 발사되는지 확인
- [ ] 투사체가 가장 가까운 적을 향하는지 확인
- [ ] 몬스터 피격 시 빨간 깜빡임 효과 확인
- [ ] 몬스터 사망 시 경험치 획득 확인

### 9.4 플레이어 테스트
- [ ] 몬스터 접촉 시 플레이어 데미지 확인
- [ ] 피격 후 무적 시간 (깜빡임) 확인
- [ ] 레벨업 시 로그 출력 확인
- [ ] 플레이어 사망 시 GameOver 상태 전환 확인

### 9.5 풀링 테스트
- [ ] Console에서 "풀 확장" 로그가 과도하게 뜨지 않는지 확인
- [ ] 몬스터/투사체가 재사용되는지 확인 (Hierarchy에서 이름 확인)

---

## 10. 문제 해결 가이드

### 몬스터가 스폰되지 않음
1. MonsterSpawner의 Default Monster Data 확인
2. PoolManager에 풀이 등록되어 있는지 확인
3. GameManager의 게임 상태가 `Playing`인지 확인

### 투사체가 발사되지 않음
1. PlayerAttack의 Projectile Prefab 확인
2. Enemy Layer Mask에 `Enemy` 레이어가 선택되어 있는지 확인
3. 적이 Detection Range 안에 있는지 확인

### 충돌이 감지되지 않음
1. 레이어 설정 확인
2. Collider2D의 Is Trigger 설정 확인
3. Rigidbody2D가 있는지 확인

### 카메라가 따라가지 않음
1. CameraFollow의 Auto Find Player 확인
2. Player 인스턴스가 씬에 있는지 확인
3. Player 스크립트의 싱글톤 Instance 확인

---

## 완료 후

모든 테스트 통과 시 Phase 2 완료!
다음 단계로 무기 시스템, 아이템, UI 등을 추가할 수 있습니다.
