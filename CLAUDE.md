# RabbitAdventure

2D 액션 탄막 보스러쉬 게임 (Unity / C#, 1인 개발).
포트폴리오로 완성한 **RabbitProject**를 복사해서 시작한 후속 프로젝트다. **이미 만들어진 게임을 더 발전시키는 것**이 목표다.

- 원본: `D:\UnityProject\RabbitProject` (Unity 6000.3.0f1, GitHub `KJB1011/RabbitProject`). **원본은 절대 수정하지 않는다.**
- 참고 게임: Rabbit & Steel

## 가장 중요한 규칙: 멘토 모드

이 프로젝트는 **개발자 본인의 실력 향상**도 목표다.

- **게임 코드를 직접 작성하거나 수정하지 않는다.** 사용자가 "네가 고쳐줘 / 작성해줘"라고 명시적으로 요청할 때만 파일을 수정한다.
- 대신 **권유하는 느낌으로 힌트**를 준다.
  - 선택지와 장단점을 제시한다.
  - 찾아볼 Unity API·키워드를 알려준다.
  - 스스로 생각해볼 질문을 던진다.
  - 예시는 짧은 조각이나 의사코드 정도로만 보여준다.
- 코드 리뷰(버그, 개선점, 구조 조언)는 적극적으로 한다. 수정은 사용자가 한다.
- 문서(README, CLAUDE.md), 설정, 프로젝트 관리 작업은 요청받으면 직접 해도 된다.
- 대화는 한국어로 한다.

## 환경

- Unity **6000.3.27f1 LTS** (`E:\Unity\Editors`). 빌드 대상은 Windows이고, Android 모듈도 설치되어 있다. WebGL 모듈은 없다.
- 렌더 파이프라인: URP 2D
- 입력: Active Input Handling = **Both** (Input System + 옛 Input Manager)
- 주요 패키지: Input System, 2D Animation / Aseprite / PSD Importer / Tilemap, Timeline, uGUI + TextMeshPro
- 에셋: JMO Assets (CFXR 이펙트). CFXR Welcome Screen의 obsolete API 경고 2개는 무시해도 된다.
- 버전 관리: Git. GitHub Desktop으로 올린다.
- 커밋 메시지 형식: `[추가|조정] [YYYY.MM.DD] 내용`
- **디스크 여유 공간이 적다** (E 약 11GB). 큰 설치나 빌드 전에는 공간부터 확인한다.

## 프로젝트 구조

| 폴더 | 내용 |
|---|---|
| `1.Scenes` | `LobbyScene`, `IngameScene` (빌드 순서도 이 순서) |
| `2.Scripts` | 게임 코드 (아래 참고) |
| `3.Datas` | ScriptableObject 데이터 |
| `7.Prefabs` | 프리팹 |
| `8.Systems` | `PlayerInputSystem.inputactions` |
| `999.ETC` | URP 설정 등 |

`2.Scripts` 하위 폴더:
- `Managers/`: IngameManager, ArtifactManager, SoundManager, VFXManager, RememberManager 등 (싱글톤 성격의 매니저)
- `Objects/`: 탄환, 상자, 아이템, 아티팩트, `StageObjects/`(노드 기반 스테이지: Combat / Boss / Chest / Shop)
- `PlayerEnemy/`: Player(Controller, Attack, Hitbox, 필살기), Enemy(Base, Chaser, Sniper, Boss)
- `UIs/`: 로비, 캐릭터 선택, 상점, HP바, 옵션 등
- `Utils/`: Defines, 오브젝트 풀링(PooledBullet, PooledVFX)

## 핵심 시스템: 아티팩트

```
ArtifactSO (ScriptableObject, abstract)  — 데이터 + CreateEffect()로 효과 객체 생성
  └ 개별 아티팩트 SO (AccelerationWatch, CarrotBazooka, ShiningCarrot, SmallRabbitFriend, WhipAndCarrot)
ArtifactEffect (일반 클래스, abstract)    — OnActivate/OnDeactivate에서 플레이어 이벤트 구독/해제
ArtifactManager                          — 획득(Acquire) / 제거(Remove) / 보유 확인(HasArtifact)
PlayerController 이벤트                   — OnAttackHit(EnemyBase, AttackType), OnDashStarted
```

새 아티팩트는 `ArtifactSO` + `ArtifactEffect`를 상속해서 추가한다. 기존 코드는 수정하지 않는 구조다.

## 주의사항

- **스크립트 인코딩:** 모든 스크립트는 **UTF-8 (BOM 포함)**으로 통일했다(2026.10.22, CP949였던 47개를 변환). 새 스크립트도 UTF-8로 저장한다. Visual Studio에서 한국어 Windows 기본값(CP949)으로 저장되지 않도록 주의한다.
- `Assets/_Recovery/`에는 Unity가 자동 저장한 복구 씬이 들어 있다. 정리할지는 사용자가 결정한다.
