using System;
using MythOfGuotaji.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 비술 카드 1개 슬롯 제어기 (개별 파일로 분리하여 Unity Script GUID 보장)
    /// </summary>
    public class BoonCardSlotUI : MonoBehaviour
    {
        [SerializeField] private Image _cardBorder;        // 희귀도 색상이 입혀지는 테두리 / 글로우
        [SerializeField] private Image _cardBackground;    // 카드 내부 배경
        [SerializeField] private Text _rarityBadgeText;    // [일반], [희귀], [영웅], [전설]
        [SerializeField] private Text _categoryText;       // [활 & 사격], [생존 & 기동] 등
        [SerializeField] private Text _titleText;          // 축복 제목 (Lv.1/5)
        [SerializeField] private Text _descriptionText;    // 조합된 포맷 설명
        [SerializeField] private Button _selectButton;     // 하단 선택 버튼
        [SerializeField] private Text _buttonLabel;        // 버튼 라벨 ("[1] 비술 선택")
        [SerializeField] private Button _cardRootButton;   // 카드 전체 클릭 지원 버튼

        private InRunBoonSO _boon;
        private Action<InRunBoonSO> _onSelectCallback;

        public void Setup(InRunBoonSO boon, Action<InRunBoonSO> onSelect, int slotNumber)
        {
            _boon = boon;
            _onSelectCallback = onSelect;

            Color rarityCol = GetRarityColor(boon.rarity);

            // 1. 테두리 및 희귀도 배지 컬러링
            if (_cardBorder != null)
            {
                _cardBorder.color = rarityCol;
                _cardBorder.raycastTarget = false;
            }

            if (_rarityBadgeText != null)
            {
                _rarityBadgeText.text = GetRarityName(boon.rarity);
                _rarityBadgeText.color = rarityCol;
                _rarityBadgeText.raycastTarget = false;
            }

            // 2. 카테고리
            if (_categoryText != null)
            {
                _categoryText.text = !string.IsNullOrEmpty(boon.category) ? $"[{boon.category}]" : "";
                _categoryText.raycastTarget = false;
            }

            // 3. 제목 (예: "곡사 사격 (Lv.1/5)")
            if (_titleText != null)
            {
                _titleText.text = boon.boonTitle;
                _titleText.color = Color.white;
                _titleText.raycastTarget = false;
            }

            // 4. 설명 (테이블 포맷팅 조합)
            if (_descriptionText != null)
            {
                _descriptionText.text = boon.description;
                _descriptionText.raycastTarget = false;
            }

            // 5. 버튼 텍스트 및 클릭 이벤트 (하단 버튼)
            if (_buttonLabel != null)
            {
                _buttonLabel.text = $"[{slotNumber}] 비술 선택";
                _buttonLabel.raycastTarget = false;
            }

            var targetBoon = boon;
            void TriggerSelect()
            {
                _onSelectCallback?.Invoke(targetBoon);
            }

            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveAllListeners();
                _selectButton.onClick.AddListener(TriggerSelect);

                var colors = _selectButton.colors;
                colors.highlightedColor = Color.Lerp(Color.white, rarityCol, 0.4f);
                _selectButton.colors = colors;
            }

            // 6. 카드 본체 전체 클릭 지원
            if (_cardRootButton != null)
            {
                _cardRootButton.onClick.RemoveAllListeners();
                _cardRootButton.onClick.AddListener(TriggerSelect);
            }
        }

        private static Color GetRarityColor(BoonRarity rarity)
        {
            return rarity switch
            {
                BoonRarity.Legendary => new Color(1f, 0.85f, 0.1f, 1f), // 노란색 / 황금
                BoonRarity.Epic => new Color(0.75f, 0.32f, 1.0f, 1f),    // 보라색
                BoonRarity.Rare => new Color(0.2f, 0.68f, 1.0f, 1f),     // 파란색
                _ => new Color(0.95f, 0.95f, 0.95f, 1f)                  // 흰색
            };
        }

        private static string GetRarityName(BoonRarity rarity)
        {
            return rarity switch
            {
                BoonRarity.Legendary => "★ 전설 ★",
                BoonRarity.Epic => "◆ 영웅 ◆",
                BoonRarity.Rare => "▲ 희귀 ▲",
                _ => "● 일반 ●"
            };
        }

        public void BindComponents(Image border, Image bg, Text rarityBadge, Text category, Text title, Text desc, Button button, Text btnLabel, Button cardRootBtn = null)
        {
            _cardBorder = border;
            _cardBackground = bg;
            _rarityBadgeText = rarityBadge;
            _categoryText = category;
            _titleText = title;
            _descriptionText = desc;
            _selectButton = button;
            _buttonLabel = btnLabel;
            _cardRootButton = cardRootBtn;
        }
    }
}
