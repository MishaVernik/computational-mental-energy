# UML-діаграми патенту

Джерела PlantUML для генерації креслень патенту.

## Типи діаграм

| Файл | Тип UML | Опис |
|------|---------|------|
| fig1_component.puml | Component | Структурна схема системи (110–210) |
| fig2_sequence.puml | Sequence | Потік даних онлайн-обчислення CME |
| fig3_activity.puml | Activity | Контур метаевристичної оптимізації |
| fig4_structure.puml | Activity (swimlane) | Формування вектора ознак |

## Генерація PNG

З кореня проекту:

```bash
python patent/generate_figures.py
```

**Залежності** (одна з опцій):

1. **pip install plantuml** – використовує PlantUML веб-сервер (рекомендовано)
2. **Java + plantuml.jar** – локальний рендеринг (швидше, без мережі)
   - Завантажити: https://plantuml.com/download
   - Розмістити `plantuml.jar` у `patent/` або `patent/diagrams/`

## Редагування

Редагуйте `.puml` файли у будь-якому текстовому редакторі. Синтаксис: [PlantUML](https://plantuml.com/).
