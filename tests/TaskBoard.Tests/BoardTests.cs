using TaskBoard.Domain;

namespace TaskBoard.Tests;

public class BoardTests
{
    private static Board NewBoard() => new(
        new Stage("Бэклог"),
        new Stage("Разработка", limit: 2),
        new Stage("Готово"));

    [Fact]
    public void Новая_задача_попадает_на_указанный_этап()
    {
        var board = NewBoard();

        board.Place(new WorkItem("1", "Форма заявки"), "Бэклог");

        Assert.Equal("Бэклог", board.StageOf("1").Name);
    }

    [Fact]
    public void Перенос_освобождает_предыдущий_этап()
    {
        var board = NewBoard();
        board.Place(new WorkItem("1", "Форма заявки"), "Бэклог");

        board.Move("1", "Разработка");

        Assert.Equal("Разработка", board.StageOf("1").Name);
        Assert.Empty(board.Stages.Single(s => s.Name == "Бэклог").Items);
    }

    [Fact]
    public void Этап_не_принимает_задачу_сверх_лимита()
    {
        var board = NewBoard();
        board.Place(new WorkItem("1", "Форма заявки"), "Разработка");
        board.Place(new WorkItem("2", "Каталог"), "Разработка");
        board.Place(new WorkItem("3", "Оплата"), "Бэклог");

        var error = Assert.Throws<WipLimitExceededException>(() => board.Move("3", "Разработка"));

        Assert.Equal("Разработка", error.Stage);
        Assert.Equal(2, error.Limit);
        Assert.Equal("Бэклог", board.StageOf("3").Name);
    }

    [Fact]
    public void Этап_без_лимита_принимает_любое_число_задач()
    {
        var board = NewBoard();

        for (var i = 0; i < 20; i++)
        {
            board.Place(new WorkItem(i.ToString(), $"Задача {i}"), "Бэклог");
        }

        Assert.Equal(20, board.Stages.Single(s => s.Name == "Бэклог").Items.Count);
    }

    [Fact]
    public void Освободившееся_место_позволяет_взять_следующую_задачу()
    {
        var board = NewBoard();
        board.Place(new WorkItem("1", "Форма заявки"), "Разработка");
        board.Place(new WorkItem("2", "Каталог"), "Разработка");
        board.Place(new WorkItem("3", "Оплата"), "Бэклог");

        board.Move("1", "Готово");
        board.Move("3", "Разработка");

        Assert.Equal("Разработка", board.StageOf("3").Name);
    }

    [Fact]
    public void Перенос_несуществующей_задачи_завершается_ошибкой()
    {
        var board = NewBoard();

        Assert.Throws<ArgumentException>(() => board.Move("нет такой", "Готово"));
    }

    [Fact]
    public void Отрицательный_лимит_этапа_недопустим()
    {
        Assert.Throws<ArgumentException>(() => new Stage("Разработка", limit: 0));
    }
}
