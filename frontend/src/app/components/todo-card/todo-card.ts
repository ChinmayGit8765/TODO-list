import { CommonModule } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { Todo } from '../../models/todo';

@Component({
  selector: 'app-todo-card',
  imports: [CommonModule],
  templateUrl: './todo-card.html'
})
export class TodoCardComponent {
  readonly todo = input.required<Todo>();
  readonly isExpanded = input(false);
  readonly isEditing = input(false);

  readonly toggleComplete = output();
  readonly toggleExpand = output();
  readonly toggleEdit = output();
  readonly remove = output();

  readonly isDone = computed(() => {
    const t = this.todo();
    if (t.subTasks.length === 0) return t.isComplete;
    return t.subTasks.every(s => s.isComplete);
  });

  readonly progress = computed(() => {
    const t = this.todo();
    return {
      done: t.subTasks.filter(s => s.isComplete).length,
      total: t.subTasks.length
    };
  });

  readonly isOverdue = computed(() => {
    const t = this.todo();
    if (!t.dueAt || this.isDone()) return false;
    return new Date(t.dueAt).getTime() < Date.now();
  });
}
