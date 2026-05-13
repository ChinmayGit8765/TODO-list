import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SubTaskListComponent } from '../../components/sub-task-list/sub-task-list';
import { TodoCardComponent } from '../../components/todo-card/todo-card';
import { TodoEditFormComponent } from '../../components/todo-edit-form/todo-edit-form';
import { Todo, TodoUpdate } from '../../models/todo';
import { TodosService } from '../../services/todos.service';

type View = 'active' | 'completed';

@Component({
  selector: 'app-todo-list',
  imports: [
    CommonModule,
    FormsModule,
    TodoCardComponent,
    TodoEditFormComponent,
    SubTaskListComponent
  ],
  templateUrl: './todo-list.html'
})
export class TodoListComponent implements OnInit {
  private readonly todosService = inject(TodosService);

  readonly todos = this.todosService.todos;
  readonly newTitle = signal('');
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly view = signal<View>('active');

  readonly expandedIds = signal<ReadonlySet<string>>(new Set());
  readonly editingId = signal<string | null>(null);

  readonly activeTodos = computed(() => this.todos().filter(t => !this.isItemDone(t)));
  readonly completedTodos = computed(() => this.todos().filter(t => this.isItemDone(t)));
  readonly visibleTodos = computed(() =>
    this.view() === 'active' ? this.activeTodos() : this.completedTodos()
  );

  async ngOnInit(): Promise<void> {
    try {
      await this.todosService.load();
    } catch {
      this.error.set('Could not load todos. Is the backend running?');
    } finally {
      this.loading.set(false);
    }
  }

  async addTodo(): Promise<void> {
    const title = this.newTitle().trim();
    if (!title) return;
    try {
      await this.todosService.add(title);
      this.newTitle.set('');
    } catch {
      this.error.set('Failed to add todo.');
    }
  }

  async onToggleComplete(todo: Todo): Promise<void> {
    try {
      await this.todosService.update(todo.id, {
        title: todo.title,
        dueAt: todo.dueAt,
        notes: todo.notes,
        isComplete: !todo.isComplete
      });
    } catch {
      this.error.set('Failed to update todo.');
    }
  }

  async onDelete(id: string): Promise<void> {
    try {
      await this.todosService.remove(id);
      this.collapse(id);
      if (this.editingId() === id) this.editingId.set(null);
    } catch {
      this.error.set('Failed to delete todo.');
    }
  }

  onToggleExpand(id: string): void {
    this.expandedIds.update(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  onToggleEdit(id: string): void {
    this.editingId.update(current => (current === id ? null : id));
  }

  async onSaveEdit(id: string, update: TodoUpdate): Promise<void> {
    try {
      await this.todosService.update(id, update);
      this.editingId.set(null);
      this.error.set(null);
    } catch {
      this.error.set('Failed to save changes.');
    }
  }

  async onAddSubTask(todoId: string, title: string): Promise<void> {
    try {
      await this.todosService.addSubTask(todoId, title);
    } catch {
      this.error.set('Failed to add subtask.');
    }
  }

  async onToggleSubTask(todoId: string, subId: string): Promise<void> {
    try {
      await this.todosService.toggleSubTask(todoId, subId);
    } catch {
      this.error.set('Failed to update subtask.');
    }
  }

  async onDeleteSubTask(todoId: string, subId: string): Promise<void> {
    try {
      await this.todosService.deleteSubTask(todoId, subId);
    } catch {
      this.error.set('Failed to delete subtask.');
    }
  }

  isExpanded(id: string): boolean {
    return this.expandedIds().has(id);
  }

  private collapse(id: string): void {
    this.expandedIds.update(prev => {
      if (!prev.has(id)) return prev;
      const next = new Set(prev);
      next.delete(id);
      return next;
    });
  }

  private isItemDone(todo: Todo): boolean {
    if (todo.subTasks.length === 0) return todo.isComplete;
    return todo.subTasks.every(s => s.isComplete);
  }
}
