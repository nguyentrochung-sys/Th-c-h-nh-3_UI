# BÀI THỰC HÀNH 4 – TÍCH HỢP GIAO DIỆN VÀO GAME ENGINE
### Flower Escape – màn gameplay chính (Combat + Xếp gear), responsive 9:21 và 3:4

Figma nguồn: file bản sao `Vũ Hữu Trọng`, trang **Thực hành 4**
- Frame **`Main`** (402×874) → trạng thái **Combat**
- Frame **`Main - Buy gear`** (402×874) → trạng thái **Xếp gear**

---

## 0. Kết quả

| | 9:21 (1080×2520) | 3:4 (1536×2048) | 9:16 (1080×1920) |
|---|---|---|---|
| Xếp gear | `Captures/9x21_GearSetup.png` | `Captures/3x4_GearSetup.png` | `Captures/9x16_GearSetup.png` |
| Combat | `Captures/9x21_Combat.png` | `Captures/3x4_Combat.png` | `Captures/9x16_Combat.png` |

Ảnh kiểm thử luồng chơi: `Captures/test_1_gear_after_actions.png`, `test_2_combat_running.png`, `test_3_back_to_gear.png`.
Self test trong Play mode: **12/12 PASS** (gộp gear, di chuyển, đổi chỗ, mua shop, START → Combat, khoá board, hết wave → về Xếp gear, nhận thưởng).

---

## 1. Phân tích đề và cách giải

| Yêu cầu | Cách làm |
|---|---|
| Tích hợp UI Figma vào Unity | Xuất **72 layer** của 2 frame ra PNG (3x–4x) qua Figma MCP, đặt đúng **X, Y, W, H** của từng layer trong Unity (đơn vị Figma × **K = 1080/402 = 2.687**). Font đúng như Figma: **Lalezar**. |
| Game mobile cơ chế xếp gear | Board 6×4 ô. Kéo thả hoa: vào ô trống thì **di chuyển**, vào hoa cùng số thì **gộp** (1+1 → 2, 2+2 → 4...), khác loại thì **đổi chỗ**. Gear đặc biệt (vũ khí, rotor, earning, stats) không gộp. Shop mua gear bằng Sun, nút Refresh. |
| Màn gameplay **2 trạng thái** | **Một bộ UI duy nhất**. Board ở frame Combat chính là board ở frame Buy gear × **0.827** (311/376), nên chuyển trạng thái = nội suy vị trí + scale board, trượt shop xuống, hiện cột booster, hiện quái và thuyền. |
| Responsive 9:21 và 3:4 | CanvasScaler **Expand** với reference 1080×2348 (= frame Figma) và **5 vùng neo**: trên-trái, trên-giữa, trên-phải, đáy, world. Ảnh nền dùng symbol 874×874 rộng hơn frame để màn 3:4 phủ kín hai bên. |

---

## 2. Cấu trúc project

```
Assets/FlowerEscape/
├── Art/Sprites/        72 PNG xuất từ Figma (đã tách nền, xem mục 4)
├── Fonts/              Lalezar-Regular.ttf (+ OFL), Lalezar SDF.asset, Lalezar SDF Outline.mat
├── Data/GearConfig.asset    sprite từng cấp gear + 5 gear đặc biệt
├── Prefabs/GearItem.prefab  Root(raycast) > Icon, Overlay, Label
├── Scenes/Gameplay.unity    scene chính (Build Settings #0)
├── Editor/
│   ├── FlowerEscapeSceneBuilder.cs   ★ dựng toàn bộ UI theo toạ độ Figma
│   ├── FlowerEscapeAutomation.cs     điều khiển Unity qua file lệnh + chụp 3 tỉ lệ
│   ├── FlowerEscapeSelfTest.cs       tự chơi thử luồng chính trong Play mode
│   ├── GameViewSizeTool.cs           thêm Game view 9:21 / 3:4 / 9:16
│   └── FlowerEscapeSpriteImporter.cs
└── Scripts/
    ├── UI/GameplayUIController.cs    ★ 2 trạng thái + chuyển cảnh
    ├── UI/ResponsiveCanvas.cs, SafeArea.cs, HudController.cs, UIFx/UIBob/UIButtonFeedback
    ├── Gear/GearBoard.cs, GearCell.cs, GearItem.cs, GearConfig.cs
    ├── Shop/ShopController.cs, ShopCard.cs
    ├── Combat/CombatSimulator.cs
    └── Core/GameSession.cs
```

---

## 3. Chạy thử

1. Mở project trong Unity 6000.3, mở scene `Assets/FlowerEscape/Scenes/Gameplay.unity`.
2. **Tools ▸ Flower Escape ▸ Add Game View Sizes** để thêm `Flower 9:21`, `Flower 3:4`, `Flower 9:16` vào dropdown của tab Game.
3. Bấm **Play**:
   - Kéo hoa số 1 vào hoa số 1 khác → thành 2. Kéo vào ô lá sen trống → di chuyển.
   - Bấm thẻ shop (5 / 10 Sun / Free) → gear mới xuất hiện ở một ô trống. **Refresh** (5 Sun) đổi hàng.
   - Bấm **+50** → nhận 50 Sun (mỗi lượt xếp gear một lần).
   - Bấm **START!** → board thu nhỏ xuống, cột booster và vùng combat hiện ra, thanh tiến trình chạy, quái mất máu.
   - Hết wave → +25 Sun, shop đổi hàng, tự về trạng thái xếp gear.
   - **x2** tăng tốc, **⚙** pause, **✕** chơi lại.
4. Đổi Game view giữa 9:21 và 3:4 **ngay khi đang Play** để thấy layout tự dàn lại.

Dựng lại scene từ đầu: **Tools ▸ Flower Escape ▸ Build Gameplay Scene**. Chụp lại ảnh: **Tools ▸ Flower Escape ▸ Capture Screenshots**. Kiểm thử: **Tools ▸ Flower Escape ▸ Run Self Test**.

---

## 4. Lấy asset từ Figma

1. Duplicate file Figma của đề về drafts (file gốc chỉ cho xem).
2. Đọc cấu trúc 2 frame (`get_metadata`) để có X, Y, W, H và tên từng layer. Đọc font, cỡ chữ, màu từng text (`get_design_context`):

   | Text | Cỡ (Figma px) | | Text | Cỡ |
   |---|---|---|---|---|
   | Level 12 | 18 | | Số trên gear | 33.86 |
   | 165 (màu `#FFE561`) | 18 | | 52.3mph / 73.3 | 14.51 |
   | Hard | 14 | | 0.24/s | 12.09 |
   | x2 | 16 | | 123m | 12 |
   | 50 (nút thưởng) | 24 | | Giá 5 / 10 / Free | 16 |
   | START! | 24 | | Refresh | 16 |
   | Số booster "3" | 18 | | Số tài nguyên | 14 |

   Font chữ: **Lalezar Regular**, chữ trắng, viền xanh than.
3. Xuất PNG từng layer (`download_assets`):
   - icon nhỏ xuất 4x, panel/nút xuất 3x;
   - layer nằm sát mép frame (boss, thanh shop) xuất từ **symbol gốc** để không bị frame cắt mép;
   - ảnh nền xuất từ symbol `environment background` 874×874.
4. **Tách nền**: ảnh xuất từ Figma MCP không có kênh alpha (instance bị phủ nền trắng, symbol bị phủ màu canvas `#1E1E1E`). Script tách nền làm 3 bước:
   - loang (flood-fill) từ mép ảnh để xoá vùng nền;
   - tính lại alpha ở dải viền 2 px theo khoảng cách màu so với màu nền;
   - khử lẫn màu nền ở viền: `C = B + (P − B)/α`.

   Nét viền đậm của art cartoon giúp việc tách rất sạch (đã kiểm tra trên nền hồng).
5. Builder tự đặt import settings cho sprite: Sprite (2D and UI), không mipmap, Alpha Is Transparency, Max Size **4096**. Không để 2048, vì ảnh rộng hơn sẽ bị thu nhỏ làm lệch phép tính bù viền. Thanh slider dùng 9-slice.

---

## 5. Kỹ thuật đặt toạ độ Figma → Unity

```
K = 1080 / 402 = 2.6866
anchor = (0, 1) của cha, pivot = (0.5, 0.5)
anchoredPosition = ((x + w/2)·K, −(y + h/2)·K)
sizeDelta        = (w·K, h·K)
```
Ảnh export lớn hơn bounds của layer (do đổ bóng/viền ngoài), nên ảnh nằm ở con `Art` và được nới ra đúng bằng phần dư:
`pad = (pngPx / scaleExport − kíchThướcNode) / 2`.

---

## 6. Responsive 9:21 và 3:4

**CanvasScaler**: Scale With Screen Size, reference **1080×2348**, **Expand**. Canvas luôn ≥ frame Figma ở cả hai chiều:

| Màn | Canvas | Phần dư dùng để |
|---|---|---|
| 9:21 (1080×2520) | 1080 × 2520 | +172 chiều cao: giãn khoảng giữa HUD và board (lộ thêm biển/nền) |
| 3:4 (1536×2048) | 1761 × 2348 | +681 chiều ngang: UI ở giữa, ảnh nền 874 rộng phủ hai bên, thanh shop dùng bản symbol 874 rộng, banner có dải đen |
| 9:16 (1080×1920) | 1321 × 2348 | tương tự 3:4 |

**5 vùng neo.** Mỗi vùng có kích thước bằng cả frame (1080×2348) và được neo vào một điểm, nên phần tử bên trong giữ nguyên khoảng cách tới mép tương ứng:

| Vùng | Neo | Chứa |
|---|---|---|
| `HudLeft` | trên-trái | ⚙, bộ đếm Sun |
| `HudCenter` | trên-giữa | Level, thanh tiến trình, đầu lâu, cờ, 123m, Hard |
| `HudRight` | trên-phải | ✕, x2, nút +50, cột tài nguyên combat |
| `WorldRegion` + `BackgroundRegion` | trên-giữa | ảnh nền, boss, quái, thuyền, nhân vật, thanh máu |
| `BottomRegion` | đáy-giữa | board, booster, shop, banner |

`SafeArea` bao các vùng tương tác, để HUD không bị tai thỏ che (thử bằng Device Simulator).

---

## 7. Hai trạng thái (`GameplayUIController`)

| Thuộc tính | t = 0 · Xếp gear (frame `Main - Buy gear`) | t = 1 · Combat (frame `Main`) |
|---|---|---|
| BoardRoot vị trí | (13, 199)·K | (12, 463)·K |
| BoardRoot scale | 1.0 | 311/376 = **0.827** |
| Shop | hiện | trượt xuống 210·K, alpha 0 |
| Nút +50 | hiện | ẩn |
| Cột booster | ẩn, lệch phải 80·K | hiện |
| Quái, thuyền, cột tài nguyên | ẩn | hiện |
| Kéo thả gear | cho phép | khoá |

Chuyển trạng thái nội suy t trong 0.45 s (EaseInOut, `unscaledDeltaTime`). Xem trước trong Editor: chuột phải component **GameplayUIController** ▸ **Preview: Combat / Preview: Gear Setup**.

---

## 8. Cơ chế xếp gear

- `GearBoard.initialLayout` lấy theo frame `Main - Buy gear`:
  ```
  2 8 . . a .
  4 r 1 4 1 .
  . . . 2 1 .
  e . . s . l
  ```
  Số là gear số; `.` là ô trống; `#` là ô khoá. Chữ là gear đặc biệt: `a` = Weapon + Heart Boom, `r` = Rotor, `e` = Earning + Regeneration, `s` = Weapon + Sunbloom, `l` = Stats + Luck.
- Lưới ô lấy từ vị trí gear trong Figma: `x = 28.72 + 57.8·cột`, `y = 333.23 + 56.03·hàng`, ô 58×58.
- Damage = tổng giá trị gear × 3.187 (bố cục ban đầu 23 × 3.187 = **73.3**, đúng như Figma).
- Gear 16/32/64 không có trong Figma, nên dùng lại sprite 1/2/4 nhân màu (sửa được trong `GearConfig.asset`).

---

## 9. Lỗi thường gặp

| Triệu chứng | Cách sửa |
|---|---|
| Chữ không hiện | Import **TMP Essential Resources** (đã có sẵn ở `Assets/TextMesh Pro`). |
| Sprite có viền trắng hoặc xám | PNG export từ Figma MCP không có alpha, phải tách nền (mục 4). |
| Phần tử lệch hoặc nhỏ đi | Max Size của texture nhỏ hơn kích thước ảnh. Builder đã đặt 4096. |
| Bấm nút không ăn | Project dùng Input System, nên EventSystem phải dùng `InputSystemUIInputModule` (scene đã có sẵn). |
| Không kéo được gear | Chỉ kéo được ở trạng thái Xếp gear, lúc Combat bị khoá (đúng luật game). |

---

## 10. Gợi ý nội dung nộp bài
1. Video: xếp gear (gộp, mua, refresh) → START! → combat → về xếp gear; đổi Game view 9:21 ↔ 3:4 khi đang Play.
2. Bộ 6 ảnh trong `Captures/` (2 trạng thái × 3 tỉ lệ) đặt cạnh ảnh frame Figma.
3. Thuyết minh: toạ độ Figma → Unity (mục 5), responsive (mục 6), 2 trạng thái dùng chung UI (mục 7).
