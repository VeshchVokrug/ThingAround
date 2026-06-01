namespace Core.Contracts;

public sealed record Category
{
    public string Value { get; init; }
    public string DisplayName { get; init; }
    public string? ParentSlug { get; init; }

    private Category(string value, string displayName, string? parent = null)
    {
        Value = value;
        DisplayName = displayName;
        ParentSlug = parent;
    }
    
    //Строительство
    public static readonly Category Construction = new("construction", "Строительство");
    public static readonly Category PowerTools = new("construction.powertools", "Электроинструменты", "construction");
    public static readonly Category HandTools = new("construction.handtools", "Ручной инструмент", "construction");
    public static readonly Category ConstructionEquip = new("construction.equipment", "Строительное оборудование", "construction");
    public static readonly Category GardenTech = new("construction.garden", "Садовая техника", "construction");

    //Спорт
    public static readonly Category Sport = new("sport", "Спорт и отдых");
    public static readonly Category WinterSport = new("sport.winter", "Зимний спорт", "sport");
    public static readonly Category Transport = new("sport.transport", "Средства передвижения", "sport");
    public static readonly Category WaterSport = new("sport.water", "Водный спорт", "sport");
    public static readonly Category Camping = new("sport.camping", "Кемпинг", "sport");
    public static readonly Category Fitness = new("sport.fitness", "Фитнес-оборудование", "sport");

    //Электроника
    public static readonly Category Electronics = new("electronics", "Электроника");
    public static readonly Category PhotoVideo = new("electronics.photo", "Фото и видео", "electronics");
    public static readonly Category Audio = new("electronics.audio", "Аудио", "electronics");
    public static readonly Category Consoles = new("electronics.consoles", "Игровые консоли", "electronics");
    public static readonly Category Projectors = new("electronics.projectors", "Проекторы и экраны", "electronics");

    //Детские товары
    public static readonly Category Kids = new("kids", "Детские товары");
    public static readonly Category BabyTransport = new("kids.transport", "Коляски и автокресла", "kids");
    public static readonly Category KidsToys = new("kids.toys", "Развивающие игрушки", "kids");
    public static readonly Category KidsFurniture = new("kids.furniture", "Детская мебель", "kids");
    public static readonly Category KidsSport = new("kids.sport", "Детский спорт", "kids");

    //Праздники
    public static readonly Category Events = new("events", "Праздники");
    public static readonly Category EventDecor = new("events.decor", "Декор и реквизит", "events");
    public static readonly Category EventSoundLights = new("events.soundlight", "Звук и свет", "events");
    public static readonly Category EventFurniture = new("events.furniture", "Мебель и посуда", "events");
    public static readonly Category EventCostumes = new("events.costumes", "Костюмы", "events");

    //Одежда
    public static readonly Category Fashion = new("fashion", "Одежда и аксессуары");
    public static readonly Category Outerwear = new("fashion.outerwear", "Верхняя одежда", "fashion");
    public static readonly Category FormalWear = new("fashion.formal", "Костюмы и вечерние наряды", "fashion");
    public static readonly Category Wedding = new("fashion.wedding", "Свадебная атрибутика", "fashion");
    public static readonly Category Accessories = new("fashion.accessories", "Аксессуары", "fashion");

    //Музыка
    public static readonly Category Music = new("music", "Музыкальное оборудование");
    public static readonly Category StringInst = new("music.strings", "Гитарные и струнные", "music");
    public static readonly Category Keyboards = new("music.keyboards", "Клавишные и синтезаторы", "music");
    public static readonly Category Drums = new("music.drums", "Ударные и перкуссия", "music");
    public static readonly Category Amps = new("music.amps", "Усилители и комбо", "music");

    //Медицина
    public static readonly Category Medical = new("medical", "Медицина");
    public static readonly Category MobilityAids = new("medical.mobility", "Костыли, ходунки, трости", "medical");
    public static readonly Category Wheelchairs = new("medical.wheelchairs", "Инвалидные коляски", "medical");
    public static readonly Category Orthopedics = new("medical.ortho", "Ортопедические изделия", "medical");
    public static readonly Category Physio = new("medical.physio", "Аппараты для физиотерапии", "medical");

    //Хобби
    public static readonly Category Hobby = new("hobby", "Хобби и творчество");
    public static readonly Category BoardGames = new("hobby.games", "Настольные игры", "hobby");
    public static readonly Category Sewing = new("hobby.sewing", "Швейное оборудование", "hobby");
    public static readonly Category ArtEquip = new("hobby.art", "Художественное оборудование", "hobby");
    
    //Список всех категорий
    public static readonly IReadOnlyList<Category> All = typeof(Category)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(f => f.FieldType == typeof(Category))
        .Select(f => (Category)f.GetValue(null)!)
        .ToList();

    public static Category? FromValue(string value) => All.FirstOrDefault(x => x.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
}