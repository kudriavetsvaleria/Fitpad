using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

[FirestoreData]
public class DishModel
{
    [FirestoreProperty]
    public string Id { get; set; }   // Уникальный идентификатор блюда

    [FirestoreProperty]
    public string UserId { get; set; }   // ID пользователя (дополнительно, хотя уже в пути Users/{userId}/Dishes)

    [FirestoreProperty]
    public string Name { get; set; }   // Название блюда

    [FirestoreProperty]
    public string CookingTime { get; set; }   // Время приготовления (строкой: "30 хв" или "45 min")

    [FirestoreProperty]
    public string Recipe { get; set; }   // Текст рецепта

    [FirestoreProperty]
    public List<string> Ingredients { get; set; }   // Список ингредиентов (названия)

    [FirestoreProperty]
    public bool IsFavorite { get; set; }   // Флаг "избранное"

    // 🔹 Новые поля для КБЖУ
    [FirestoreProperty]
    public double CaloriesPerUnit { get; set; }   // Калории (например на 100 г)

    [FirestoreProperty]
    public double ProteinPerUnit { get; set; }   // Белки (на 100 г)

    [FirestoreProperty]
    public double FatPerUnit { get; set; }   // Жиры (на 100 г)

    [FirestoreProperty]
    public double CarbPerUnit { get; set; }   // Углеводы (на 100 г)

    [FirestoreProperty]
    public double DefaultServingGrams { get; set; }   // Размер порции по умолчанию (в граммах)

    // 🔹 Метки времени
    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; }   // Когда добавлено

    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; }   // Когда обновлено
}
