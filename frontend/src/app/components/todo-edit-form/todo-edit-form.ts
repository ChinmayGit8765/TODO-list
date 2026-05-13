import { CommonModule } from '@angular/common';
import { Component, effect, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Todo, TodoUpdate } from '../../models/todo';

@Component({
  selector: 'app-todo-edit-form',
  imports: [CommonModule, FormsModule],
  templateUrl: './todo-edit-form.html'
})
export class TodoEditFormComponent {
  readonly todo = input.required<Todo>();

  readonly save = output<TodoUpdate>();
  readonly cancel = output();

  readonly draftTitle = signal('');
  readonly draftDueAt = signal('');
  readonly draftNotes = signal('');
  readonly error = signal<string | null>(null);

  constructor() {
    // Re-initialize the draft whenever the bound todo changes (e.g. when the
    // user starts editing a different row).
    effect(() => {
      const t = this.todo();
      this.draftTitle.set(t.title);
      this.draftDueAt.set(t.dueAt ? t.dueAt.slice(0, 10) : '');
      this.draftNotes.set(t.notes ?? '');
      this.error.set(null);
    });
  }

  onSubmit(): void {
    const title = this.draftTitle().trim();
    if (!title) {
      this.error.set('Title is required.');
      return;
    }
    this.save.emit({
      title,
      dueAt: this.draftDueAt() ? new Date(this.draftDueAt()).toISOString() : null,
      notes: this.draftNotes().trim() || null,
      isComplete: this.todo().isComplete
    });
  }
}
