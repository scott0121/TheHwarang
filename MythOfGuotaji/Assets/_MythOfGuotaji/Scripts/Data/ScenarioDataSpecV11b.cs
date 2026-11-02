using System.Collections.Generic;

namespace MythOfGuotaji.Data
{
    public class StageBriefingInfo
    {
        public int chapter;
        public int globalStageNumber; // 1 ~ 22
        public string stageCode;      // "0-1", "1-1" ... "6-3"
        public string stageTitle;
        public string bossOrKeeperName;
        public string gimmickSummary;
        public string scenarioNote;
    }

    public class CoreMemoryTraitInfo
    {
        public string id;
        public string title;
        public string unlockCondition;
        public string effectDescription;
        public string loreQuote;
    }

    /// <summary>
    /// 기획서 v1.1b 스펙에 기반한 22개 스테이지 브리핑 및 6대 핵심 기억 데이터베이스
    /// </summary>
    public static class ScenarioDataSpecV11b
    {
        public static (List<StageBriefingInfo> stages, List<CoreMemoryTraitInfo> memories) LoadData()
        {
            var _stages = new List<StageBriefingInfo>();
            var _coreMemories = new List<CoreMemoryTraitInfo>();

        {
            _stages.Clear();

            // 챕터 0: 폭풍우와 괴신선 (튜토리얼, 1스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 0, globalStageNumber = 1, stageCode = "0-1",
                stageTitle = "폭풍우와 괴신선 (튜토리얼)",
                bossOrKeeperName = "괴신선 갑판 요괴 무리",
                gimmickSummary = "선형 4개 방 구성 — 기본 이동·회피(슬라이딩), 활 5연사/차징 사격, 단검 패링, 보옥의 영혼석 수집 튜토리얼",
                scenarioNote = "서기 890년, 당나라로 향하던 사절단 선박이 진성여왕의 기억을 노린 검은 구미호의 폭풍에 난파되고, 거타지는 곡도(鵠島) 해안에서 눈을 뜬다."
            });

            // 챕터 1: 안개 낀 해안과 부러진 목궁 (3스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 1, globalStageNumber = 2, stageCode = "1-1",
                stageTitle = "안개 낀 곡도 해안",
                bossOrKeeperName = "수문장: 갑각 해안 수호병",
                gimmickSummary = "해안 안개 지형 / 기본 돌진 요괴 & 맹독충 출현 — 단검으로 독침을 흘려보내고 열쇠를 확보하라",
                scenarioNote = "바위 틈에 갇혀 있던 보석 토끼 '보옥'을 구출한 거타지. 보옥은 자신이 여왕의 여의보주임을 숨긴 채 동행을 제안한다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 1, globalStageNumber = 3, stageCode = "1-2",
                stageTitle = "부러진 목궁의 기억",
                bossOrKeeperName = "수문장: 정예 암석 괴수",
                gimmickSummary = "정예 암석 굴리기 기믹 / 클리어 시 핵심 기억 [초심의 시위] 해금",
                scenarioNote = "어린 시절 처음 활시위를 당기던 거타지의 기억과, 그를 호위무사로 발탁했던 진성여왕의 첫인상이 겹쳐진다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 1, globalStageNumber = 4, stageCode = "1-3",
                stageTitle = "망루의 눈먼 파수꾼 (챕터 1 보스전)",
                bossOrKeeperName = "보스: 눈먼 파수꾼 석진(石鎭)",
                gimmickSummary = "둔중한 철퇴 파동과 투석 탄막 — 푸른 투사체를 단검 패링으로 되돌려 석진의 흉갑(기억의 핵)을 노출시켜라!",
                scenarioNote = "과거 곡도 사당을 지키다 여우의 요기에 눈이 먼 거인 무사 석진을 정화하고 첫 번째 진실의 파편을 회수한다."
            });

            // 챕터 2: 불타는 서라벌의 환영 (4스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 2, globalStageNumber = 5, stageCode = "2-1",
                stageTitle = "잿빛 회랑의 화염",
                bossOrKeeperName = "수문장: 도깨비 화염술사 우두머리",
                gimmickSummary = "도깨비불 패링 핑퐁 기믹 — 느리게 날아오는 도깨비 화염구를 단검으로 쳐내어 보호막을 분쇄하라",
                scenarioNote = "미궁 내부가 불타는 서라벌 왕경의 환영으로 일렁인다. 거타지는 자신이 떠나온 날 밤의 불길을 기억해낸다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 2, globalStageNumber = 6, stageCode = "2-2",
                stageTitle = "무너진 전각과 화랑의 맹세",
                bossOrKeeperName = "수문장: 저돌맹진 중장 멧돼지",
                gimmickSummary = "돌진 유도 후 벽 격돌 기절 기믹 — 화랑 후배 '호원' 구출 이벤트 발생",
                scenarioNote = "환영 속에서 헤매던 화랑 후배 호원과 재회한다. 호원은 거타지가 반역자로 몰렸다는 충격적인 소식을 전한다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 2, globalStageNumber = 7, stageCode = "2-3",
                stageTitle = "붉은 화살의 누명",
                bossOrKeeperName = "수문장: 화염 주(鴸) 군단장",
                gimmickSummary = "2초 곡사 폭격 장판 / 클리어 시 핵심 기억 [흔들리지 않는 눈] 해금",
                scenarioNote = "거타지의 기억 속 '여왕을 겨눈 붉은 화살'은 암살 시도가 아니라, 여왕의 그림자에 숨어든 요괴를 쏘아 맞힌 것이었음이 드러난다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 2, globalStageNumber = 8, stageCode = "2-4",
                stageTitle = "단죄의 화염 (챕터 2 보스전)",
                bossOrKeeperName = "보스: 붉은 집행관(赤執行官)",
                gimmickSummary = "화염 검기 연속 발사 & 십자 화염 기둥 — 강화(황금) 검기를 패링 반사하여 그로기를 유도하고 완전 충전 화살로 격파!",
                scenarioNote = "왜곡된 죄책감이 구현화한 붉은 집행관을 쓰러뜨리고, 거타지는 스스로의 활에 대한 의심을 거둔다."
            });

            // 챕터 3: 통곡의 나루터 (4스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 3, globalStageNumber = 9, stageCode = "3-1",
                stageTitle = "안개 잠긴 나루터",
                bossOrKeeperName = "수문장: 진흙 이괴 수호자",
                gimmickSummary = "늪지 감속 장판 & 요선 거미 견인 사슬 — 슬라이딩(무적 회피)으로 거미줄을 즉시 절단하라",
                scenarioNote = "침몰한 사절단원들의 원혼이 떠도는 통곡의 나루터. 늙은 바다거북 '해로'가 물길을 안내한다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 3, globalStageNumber = 10, stageCode = "3-2",
                stageTitle = "가라앉은 갑판의 흔적",
                bossOrKeeperName = "수문장: 흑철 방패수문장",
                gimmickSummary = "전면 방패 방어 — 슬라이딩으로 배후를 잡거나 패링 반사탄으로 방패를 무력화하라",
                scenarioNote = "난파 당시 동료들을 구하지 못했다는 거타지의 회한이 시험대에 오른다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 3, globalStageNumber = 11, stageCode = "3-3",
                stageTitle = "노승 부부의 탄식",
                bossOrKeeperName = "수문장: 심해 독충 여왕",
                gimmickSummary = "다중 산탄 독침 탄막 / 클리어 시 핵심 기억 [회한의 무게] 해금",
                scenarioNote = "곡도 사당의 노승 부부가 사실 서해 용왕과 용비의 화신이었으며, 여우가 그들의 간을 노리고 있음이 밝혀진다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 3, globalStageNumber = 12, stageCode = "3-4",
                stageTitle = "원한의 물길 (챕터 3 보스전)",
                bossOrKeeperName = "보스: 원한의 뱃사공 양패(兩貝)",
                gimmickSummary = "파도 밀치기 & 원혼 구체 소환 — 원혼 구체를 단검으로 쳐내어 양패의 등불(기억의 핵)을 정화하라!",
                scenarioNote = "바다에 수장된 사절단의 한을 품은 양패를 진혼하고, 보옥은 거타지에게 조금씩 마음을 열기 시작한다."
            });

            // 챕터 4: 천년의 여우굴 (4스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 4, globalStageNumber = 13, stageCode = "4-1",
                stageTitle = "환영이 깃든 여우숲",
                bossOrKeeperName = "수문장: 환영 여우귀 수석",
                gimmickSummary = "피격 시 2체 분신 생성 기믹 — 보옥의 빛(또는 차징 관통 화살)으로 본체와 분신을 동시에 꿰뚫어라",
                scenarioNote = "여우굴 초입, 보옥의 목소리와 진성여왕의 목소리가 완벽히 겹쳐 들리기 시작한다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 4, globalStageNumber = 14, stageCode = "4-2",
                stageTitle = "거짓된 밀실",
                bossOrKeeperName = "수문장: 영력 흡수 괴물",
                gimmickSummary = "원거리 사격 흡수 배리어 — 근접 단검 베기로 배리어를 파쇄한 뒤 사격하라",
                scenarioNote = "천년 묵은 여우가 왜 진성여왕의 기억과 용왕의 일족을 동시에 노렸는지 단서가 드러난다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 4, globalStageNumber = 15, stageCode = "4-3",
                stageTitle = "여의보주의 진실",
                bossOrKeeperName = "수문장: 사방신 수호 토템",
                gimmickSummary = "일반 공격 면역 토템 — 토템이 발사하는 황금 탄환을 단검 패링(2배 피해)하여 파괴! 핵심 기억 [의연함] 해금",
                scenarioNote = "보옥은 단순한 보물이 아니라, 진성여왕이 자신의 생명력과 기억을 쪼개어 거타지를 지키기 위해 만든 분신이었다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 4, globalStageNumber = 16, stageCode = "4-4",
                stageTitle = "천년 여우의 그림자 (챕터 4 보스전)",
                bossOrKeeperName = "보스: 천년 여우의 그림자 연호(煙狐)",
                gimmickSummary = "3방향 환영 분신 탄막 & 부채꼴 진공 흡입 — 진짜 연호의 꼬리 핵을 간파하여 완전 충전 화살을 꽂아라!",
                scenarioNote = "여우의 분신 연호를 격파하지만, 본체인 검은 구미호는 이미 심연의 제단에서 용왕의 영역을 침식하고 있었다."
            });

            // 챕터 5: 심연의 제단 (4스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 5, globalStageNumber = 17, stageCode = "5-1",
                stageTitle = "침식된 해저 신전",
                bossOrKeeperName = "수문장: 심연 철갑 거북",
                gimmickSummary = "20m 가속 돌진 & 3m 반경 충격파 — 지형 기둥 뒤로 회피하여 돌진을 멈추게 한 뒤 반격하라",
                scenarioNote = "서해 용궁으로 이어지는 심연의 제단. 흑기(黑氣)에 오염된 신수들이 거타지의 앞길을 막는다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 5, globalStageNumber = 18, stageCode = "5-2",
                stageTitle = "용비의 결계",
                bossOrKeeperName = "수문장: 자폭 화염조 군단",
                gimmickSummary = "고속 자폭 참새 떼 & 곡사 폭격 복합 웨이브",
                scenarioNote = "용왕 부부가 마지막 힘으로 제단의 봉인을 지탱하고 있다. 거타지는 신궁의 경지에 다다른다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 5, globalStageNumber = 19, stageCode = "5-3",
                stageTitle = "신궁(神弓)의 각성",
                bossOrKeeperName = "수문장: 심연 수문장 쌍귀",
                gimmickSummary = "복합 정예 수문장 2기 동시 교전 / 클리어 시 핵심 기억 [신궁(神弓)] 해금",
                scenarioNote = "“내 화살은 죽이기 위한 것이 아니라, 이어진 인연을 되돌리기 위한 것이다.”"
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 5, globalStageNumber = 20, stageCode = "5-4",
                stageTitle = "타락한 흑룡 (챕터 5 보스전)",
                bossOrKeeperName = "보스: 타락한 흑룡 청잠(靑潛)",
                gimmickSummary = "거대 브레스 장판 & 뇌전 탄막 — 강화 뇌전구를 연속 패링하여 흑룡의 역린(기억의 핵)을 꿰뚫어라!",
                scenarioNote = "여우의 주술에 잠식당했던 용왕의 아들 청잠을 정화하고, 용왕으로부터 최후의 비술과 축복을 받는다."
            });

            // 챕터 6: 새벽을 여는 시위 (3스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 6, globalStageNumber = 21, stageCode = "6-1",
                stageTitle = "진홍의 영역",
                bossOrKeeperName = "수문장: 구미호의 친위 요괴장",
                gimmickSummary = "전 기믹 총집결 웨이브 — 도깨비불, 사방신 토템, 거미 사슬, 암석 돌진 복합 돌파",
                scenarioNote = "스물한 개의 기억 조각이 하나로 모이며, 거타지와 보옥은 모든 진실을 마주한 채 최후의 결전지로 향한다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 6, globalStageNumber = 22, stageCode = "6-2",
                stageTitle = "인연의 끝과 시작 (최종장 전초 & 진엔딩 결전)",
                bossOrKeeperName = "최종 보스: 진홍의 주인 검은 구미호 (챕터 6-2 / 6-3)",
                gimmickSummary = "9꼬리 페이즈 전환 (부채꼴 진공 흡입 + 180도 휩쓸기 + 여우불 탄막 패링) / 22개 기억 해금 시 [새벽의 시위] 진엔딩!",
                scenarioNote = "보옥이 자신을 희생해 여우를 봉인하려는 순간, 모든 기억을 되찾은 거타지가 '새벽의 시위'를 당겨 여우의 요기만을 꿰뚫고 보옥과 여왕의 혼을 함께 구원한다."
            });

            // 6대 핵심 기억 (기획서 5.2장)
            _coreMemories.Clear();
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH1_FIRST_STRING",
                title = "1. 초심의 시위 (챕터 1 핵심 기억)",
                unlockCondition = "챕터 1-2 해금 (기본 해금)",
                effectDescription = "전투 진입 후 첫 화살 및 완전 충전 사격의 치명타 확률 +20% 증가",
                loreQuote = "“시위를 당길 때 손끝이 떨리는 것은 두려움 때문이 아니다. 지켜야 할 이의 숨결이 시위에 실려 있기 때문이다.”"
            });
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH2_STEADY_EYE",
                title = "2. 흔들리지 않는 눈 (챕터 2 핵심 기억)",
                unlockCondition = "챕터 2-3 해금",
                effectDescription = "완전 충전 화살 적중 시 보스/수문장의 약점(기억의 핵) 노출 시간 +1.5초 연장",
                loreQuote = "“불타는 왕경 한복판에서도 내가 겨눈 것은 여왕의 심장이 아니라, 그 뒤편에 도사린 검은 그림자였다.”"
            });
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH3_REGRET_WEIGHT",
                title = "3. 회한의 무게 (챕터 3 핵심 기억)",
                unlockCondition = "챕터 3-3 해금",
                effectDescription = "단검 베기 및 투사체 패링 성공 시 보옥 보호막 충전량 및 이동속도 보너스 증가",
                loreQuote = "“가라앉은 전우들의 이름을 잊지 않겠다. 부러진 화살촉을 벼려 만든 이 단검이 내 두 번째 맹세다.”"
            });
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH4_RESOLUTE",
                title = "4. 의연함 (챕터 4 핵심 기억)",
                unlockCondition = "챕터 4-3 해금",
                effectDescription = "환영·분신 요괴 간파 및 본체에 대한 최종 피해량 +25% 증가",
                loreQuote = "“천 개의 거짓 목소리가 속삭여도, 내 뒤를 따르는 작은 토끼의 발소리 하나만은 속일 수 없다.”"
            });
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH5_DIVINE_BOW",
                title = "5. 신궁(神弓) (챕터 5 핵심 기억)",
                unlockCondition = "챕터 5-3 해금",
                effectDescription = "완전 충전(25m) 화살이 적의 방패·배리어를 관통하며 넉백 위력 +35% 증가",
                loreQuote = "“바다를 가르고 먹구름을 꿰뚫는 한 발. 삼국유사에 기록된 명궁 거타지의 진정한 각성.”"
            });
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH6_DAWN_STRING",
                title = "6. 새벽의 시위 (챕터 6 진엔딩 핵심 기억)",
                unlockCondition = "기억의 조각 22개 전체 회수",
                effectDescription = "모든 무기와 비술의 재사용 대기시간 감소 및 진엔딩 전용 황금 인연 화살 발동",
                loreQuote = "“보옥아, 너를 부수고 얻는 평화는 내 답이 아니다. 이 화살로 악연만 끊고, 인연은 남긴다!”"
            });
        }



            return (_stages, _coreMemories);
        }
    }
}
