# RabbitAdventure

2D 액션 탄막 보스러쉬 게임 · Unity / C# · 1인 개발

포트폴리오로 완성한 **[RabbitProject](https://github.com/KJB1011/RabbitProject)**를 이어받아 더 발전시키는 프로젝트입니다.
RabbitProject에서 만든 **아티팩트 시스템**과 유저 테스트로 다듬은 게임 루프를 기반으로, 콘텐츠와 구조를 한 단계 더 확장하는 것이 목표입니다.

> RabbitProject는 [Rabbit & Steel](https://store.steampowered.com/app/2126090/Rabbit_and_Steel/)을 참고해 제작했습니다.

## 프로젝트 히스토리

| 단계 | 기간 | 내용 |
|---|---|---|
| **RabbitProject** | 2026.04 ~ 2026.08 | 핵심 전투, 아티팩트 시스템, 노드형 스테이지 구현 → itch.io 배포 및 유저 테스트 |
| **RabbitAdventure** | 2026.10 ~ 진행 중 | RabbitProject를 기반으로 기능 확장 및 구조 개선 |

## RabbitAdventure에서 달라진 점

| 항목 | 내용 | 상태 |
|---|---|---|
| 엔진 업그레이드 | Unity 6000.3.0f1 → **6000.3.27f1 LTS** (장기 지원 버전으로 안정성 확보) | ✅ 완료 |
| <!-- TODO: 추가할 기능 --> | <!-- TODO: 무엇을, 왜 --> | 🚧 진행 예정 |

<!-- TODO: 개발하면서 이 표를 채워주세요. 기능만 적기보다 "왜 바꿨는지(문제)"와 "어떻게 바꿨는지(해결)"를 함께 적으면 포트폴리오로서 훨씬 설득력이 있습니다. -->

## Play / Demo (이전 버전: RabbitProject)

- 게임플레이 영상: https://youtu.be/AVceYGtCO7s
- 브라우저에서 플레이: https://kjb001011.itch.io/rabbitprojecttest

<!-- TODO: 게임플레이 GIF를 docs/gameplay.gif 경로에 추가한 뒤 아래 줄의 주석을 해제하세요. -->
<!-- ![gameplay](docs/gameplay.gif) -->

## 게임 구성

- **노드형 스테이지:** 전투 / 보스 / 상자 / 상점 노드를 골라 진행
- **적:** 추적형(Chaser), 원거리 저격형(Sniper), 탄막 패턴을 쓰는 보스
- **전투:** 기본 공격, 대시, 필살기, 예고 표시(Telegraph)가 있는 탄막 회피
- **성장:** 상자와 상점에서 아티팩트를 얻어 캐릭터 강화

## 핵심 시스템: 아티팩트(Artifact) 시스템

캐릭터에게 "공격 적중 시 추가 효과", "대시 시 효과 발동" 같은 서로 다른 효과를 부여하는 시스템입니다. 아티팩트 종류가 늘어날수록 조합의 수도 함께 늘어나기 때문에, **새로운 아티팩트를 추가해도 기존 코드를 건드리지 않도록** 설계하는 것이 핵심 과제였습니다.

- **데이터와 로직 분리:** 아티팩트 데이터는 `ScriptableObject`(`ArtifactSO`)로, 실제 효과는 런타임 객체(`ArtifactEffect`)로 분리
- **이벤트 기반 동작:** 각 효과는 플레이어의 이벤트(`OnAttackHit`, `OnDashStarted`)를 구독해서 자신에게 필요한 순간에만 반응하므로, 효과끼리 서로 몰라도 자유롭게 조합됨
- **확장 방법:** 새 아티팩트는 두 클래스를 상속해 필요한 부분만 구현하면 됨

```
ArtifactSO (ScriptableObject)
  └─ CreateEffect() ──▶ ArtifactEffect
                          ├─ OnActivate()   : 플레이어 이벤트 구독
                          └─ OnDeactivate() : 구독 해제
ArtifactManager : 아티팩트 획득 / 제거 / 보유 확인
PlayerController : OnAttackHit, OnDashStarted 이벤트 발행
```

현재 구현된 아티팩트: 가속의 회중시계, 당근 바주카, 빛나는 당근, 작은 토끼 친구, 채찍과 당근

## 유저 테스트 기반 개선

RabbitProject를 itch.io에 배포해 지인들에게 플레이 테스트를 받았습니다. 게임성에 대한 반응은 좋았지만 일부 보스 구간에서 난이도 때문에 어려움을 겪는 유저가 있었습니다. 이 피드백을 반영해 **사망 시 스테이지 처음이 아닌 해당 구간에서 재시작할 수 있는 중간 세이브 기능**을 추가했습니다.

## Tech Stack

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.27f1 LTS (URP 2D) |
| Language | C# |
| 주요 기술 | ScriptableObject 기반 데이터 설계, 이벤트 기반 효과 시스템, 오브젝트 풀링(GC 최적화), 상속 기반 모듈화 |
| 입력 | Unity Input System |
| 배포 | itch.io (RabbitProject) |

## 담당 범위

1인 개발로 기획, 프로그래밍, 시스템 설계 전 과정을 직접 담당했습니다.

## 실행 방법

에디터에서 실행하려면:

1. Unity Hub에서 이 저장소 폴더를 추가하고 **Unity 6000.3.27f1**로 엽니다.
2. `Assets/1.Scenes/LobbyScene`을 열고 Play를 누릅니다.

<!-- TODO: RabbitAdventure 빌드를 배포하면 다운로드 링크를 추가하세요. -->

## 배운 점

**RabbitProject에서:** 기능 하나하나를 따로 만드는 것보다, 앞으로 늘어날 경우의 수를 먼저 고려해 확장 가능한 구조로 설계하는 것이 장기적으로 훨씬 효율적이라는 것을 배웠습니다. 또한 스스로 만족스러운 결과물도 실제 사용자에게는 다르게 다가올 수 있다는 것을, 직접 유저 테스트를 돌려보며 체감했습니다.

<!-- TODO: RabbitAdventure를 진행하면서 배운 점을 추가하세요. -->
