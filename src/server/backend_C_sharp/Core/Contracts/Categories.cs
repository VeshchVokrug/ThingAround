namespace Core.Contracts;

public sealed record Category
{
    public string Name { get; init; }
    public string Value { get; init; }
    public string DisplayName { get; init; }
    public string? ParentSlug { get; init; }

    private Category(string name, string value, string displayName, string? parent = null)
    {
        Name = name;
        Value = value;
        DisplayName = displayName;
        ParentSlug = parent;
    }
    
    //Строительство
    public static readonly Category Construction = new("Construction", "construction", "Строительство");
    public static readonly Category PowerTools = new("PowerTools", "construction.powertools", "Электроинструменты", "construction");
    public static readonly Category HandTools = new("HandTools", "construction.handtools", "Ручной инструмент", "construction");
    public static readonly Category ConstructionEquip = new("ConstructionEquip", "construction.equipment", "Строительное оборудование", "construction");
    public static readonly Category GardenTech = new("GardenTech", "construction.garden", "Садовая техника", "construction");

    //Спорт
    public static readonly Category Sport = new("Sport", "sport", "Спорт и отдых");
    public static readonly Category WinterSport = new("WinterSport", "sport.winter", "Зимний спорт", "sport");
    public static readonly Category Transport = new("Transport", "sport.transport", "Средства передвижения", "sport");
    public static readonly Category WaterSport = new("WaterSport", "sport.water", "Водный спорт", "sport");
    public static readonly Category Camping = new("Camping", "sport.camping", "Кемпинг", "sport");
    public static readonly Category Fitness = new("Fitness", "sport.fitness", "Фитнес-оборудование", "sport");

    //Электроника
    public static readonly Category Electronics = new("Electronics", "electronics", "Электроника");
    public static readonly Category PhotoVideo = new("PhotoVideo", "electronics.photo", "Фото и видео", "electronics");
    public static readonly Category Audio = new("Audio", "electronics.audio", "Аудио", "electronics");
    public static readonly Category Consoles = new("Consoles", "electronics.consoles", "Игровые консоли", "electronics");
    public static readonly Category Projectors = new("Projectors", "electronics.projectors", "Проекторы и экраны", "electronics");

    //Детские товары
    public static readonly Category Kids = new("Kids", "kids", "Детские товары");
    public static readonly Category BabyTransport = new("BabyTransport", "kids.transport", "Коляски и автокресла", "kids");
    public static readonly Category KidsToys = new("KidsToys", "kids.toys", "Развивающие игрушки", "kids");
    public static readonly Category KidsFurniture = new("KidsFurniture", "kids.furniture", "Детская мебель", "kids");
    public static readonly Category KidsSport = new("KidsSport", "kids.sport", "Детский спорт", "kids");

    //Праздники
    public static readonly Category Events = new("Events", "events", "Праздники");
    public static readonly Category EventDecor = new("EventDecor", "events.decor", "Декор и реквизит", "events");
    public static readonly Category EventSoundLights = new("EventAV", "events.soundlight", "Звук и свет", "events");
    public static readonly Category EventFurniture = new("EventFurniture", "events.furniture", "Мебель и посуда", "events");
    public static readonly Category EventCostumes = new("Costumes", "events.costumes", "Костюмы", "events");

    //Одежда
    public static readonly Category Fashion = new("Fashion", "fashion", "Одежда и аксессуары");
    public static readonly Category Outerwear = new("Outerwear", "fashion.outerwear", "Верхняя одежда", "fashion");
    public static readonly Category FormalWear = new("FormalWear", "fashion.formal", "Костюмы и вечерние наряды", "fashion");
    public static readonly Category Wedding = new("Wedding", "fashion.wedding", "Свадебная атрибутика", "fashion");
    public static readonly Category Accessories = new("Accessories", "fashion.accessories", "Аксессуары", "fashion");

    //Музыка
    public static readonly Category Music = new("Music", "music", "Музыкальное оборудование");
    public static readonly Category StringInst = new("StringInst", "music.strings", "Гитарные и струнные", "music");
    public static readonly Category Keyboards = new("Keyboards", "music.keyboards", "Клавишные и синтезаторы", "music");
    public static readonly Category Drums = new("Drums", "music.drums", "Ударные и перкуссия", "music");
    public static readonly Category Amps = new("Amps", "music.amps", "Усилители и комбо", "music");

    //Медицина
    public static readonly Category Medical = new("Medical", "medical", "Медицина");
    public static readonly Category MobilityAids = new("MobilityAids", "medical.mobility", "Костыли, ходунки, трости", "medical");
    public static readonly Category Wheelchairs = new("Wheelchairs", "medical.wheelchairs", "Инвалидные коляски", "medical");
    public static readonly Category Orthopedics = new("Orthopedics", "medical.ortho", "Ортопедические изделия", "medical");
    public static readonly Category Physio = new("Physio", "medical.physio", "Аппараты для физиотерапии", "medical");

    //Хобби
    public static readonly Category Hobby = new("Hobby", "hobby", "Хобби и творчество");
    public static readonly Category BoardGames = new("BoardGames", "hobby.games", "Настольные игры", "hobby");
    public static readonly Category Sewing = new("Sewing", "hobby.sewing", "Швейное оборудование", "hobby");
    public static readonly Category ArtEquip = new("ArtEquip", "hobby.art", "Художественное оборудование", "hobby");
    
    //Список всех категорий
    public static readonly IReadOnlyList<Category> All = typeof(Category)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(f => f.FieldType == typeof(Category))
        .Select(f => (Category)f.GetValue(null)!)
        .ToList();

    public static Category? FromValue(string value) => All.FirstOrDefault(x => x.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
}