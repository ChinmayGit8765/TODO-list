import { CommonModule } from '@angular/common';
import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Todo } from '../../models/todo';

@Component({
  selector: 'app-sub-task-list',
  imports: [CommonModule, FormsModule],
  templateUrl: './sub-task-list.html'
})
export class SubTaskListComponent {
  readonly todo = input.required<Todo>();

  readonly toggleSub = output<string>();
  readonly removeSub = output<string>();
  readonly addSub = output<string>();

  readonly draftTitle = signal('');

  onAdd(): void {
    const title = this.draftTitle().trim();
    if (!title) return;
    this.addSub.emit(title);
    this.draftTitle.set('');
  }
}
