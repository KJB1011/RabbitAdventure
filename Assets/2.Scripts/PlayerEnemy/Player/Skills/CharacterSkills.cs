using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 캐릭터마다 다른 스킬(기본기·특수기·필살기)의 공통 틀.
/// 플레이어 프리팹에 PlayerController와 함께 붙이고, 캐릭터별로 이 클래스를 상속해서 만든다.
///   - RabbitSwordSkills  : 토끼 검사
///   - RabbitGunnerSkills : 토끼 총잡이
///
/// PlayerController가 맡는 것 : 이동, HP, 쿨타임, 입력, 회피기(구르기), 무적
/// 이 클래스가 맡는 것        : 기본기·특수기·필살기를 "어떻게" 쓰는지
/// </summary>
[RequireComponent(typeof(PlayerController))]
public abstract class CharacterSkills : MonoBehaviour
{
    protected PlayerController Player { get; private set; }

    protected virtual void Awake() => Player = GetComponent<PlayerController>();

    /// <summary>플레이어가 생성되고 이름 등을 설정할 때 (PlayerController.SetInitChar)</summary>
    public virtual void OnInit() { }

    // ── 기본기 ──
    /// <summary>지금 기본기를 쓸 수 있는지 (총잡이: 총알이 있고 재장전 중이 아닐 때)</summary>
    public virtual bool CanUseBasic => true;
    /// <summary>기본기 쿨타임 배율 (총잡이 쌍권총: 0.5)</summary>
    public virtual float BasicCooldownScale => 1f;
    /// <summary>기본기 실행. 공격이 끝나면 onDone을 불러야 한다 (공격 중 상태 해제)</summary>
    public abstract void StartBasic(Action onDone);

    // ── 특수기 ──
    public virtual bool CanUseSuper => true;
    /// <summary>특수기 동작 (공격 중 상태 설정·해제까지 이 코루틴이 맡는다)</summary>
    public abstract IEnumerator SuperRoutine();

    // ── 필살기 ──
    /// <summary>필살기 동작 (공격 중 상태 설정·해제까지 이 코루틴이 맡는다)</summary>
    public abstract IEnumerator SpecialRoutine();

    // ── 회피기 (공통 동작 + 캐릭터별 추가 효과) ──
    /// <summary>true면 회피기가 커서 반대쪽으로 최대 거리만큼 (총잡이 백스텝 산탄)</summary>
    public virtual bool DashAwayFromCursor => false;
    /// <summary>회피기가 출발하는 순간 (총잡이: 산탄 발사, 탄피 폭탄 설치)</summary>
    public virtual void OnDashStarted(Vector2 startPos) { }
    /// <summary>회피기가 끝난 순간 (총잡이: 재장전)</summary>
    public virtual void OnDashFinished() { }

    // ── 숙련도 강화 ──
    /// <summary>
    /// 이 캐릭터 전용 강화를 켠다. 처리했으면 true
    /// (false면 PlayerController가 공통 강화 컴포넌트 PlayerSkillUpgrades로 넘긴다 — 토끼 검사의 강화들)
    /// 어떤 캐릭터가 어떤 강화를 고를 수 있는지는 SkillMasteryManager의 트랙(characterId)에서 정한다.
    /// </summary>
    public virtual bool ApplyUpgrade(SkillUpgradeType type) => false;

    // ── 상태 정리 ──
    /// <summary>노드 이동 시작 (진행 중인 스킬 정리)</summary>
    public virtual void OnNodeMove() { }
    /// <summary>노드 재시작 (게임오버 후)</summary>
    public virtual void OnResetState() { }
}
