import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Todo, TodoUpdate } from '../models/todo';

@Injectable({ providedIn: 'root' })
export class TodosService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/todos';

  private readonly _todos = signal<Todo[]>([]);
  readonly todos = this._todos.asReadonly();
  readonly count = computed(() => this._todos().length);

  async load(): Promise<void> {
    const todos = await firstValueFrom(this.http.get<Todo[]>(this.baseUrl));
    this._todos.set(todos);
  }

  async add(title: string): Promise<Todo> {
    const created = await firstValueFrom(
      this.http.post<Todo>(this.baseUrl, { title })
    );
    this._todos.update(list => [...list, created]);
    return created;
  }

  async update(id: string, update: TodoUpdate): Promise<Todo> {
    const updated = await firstValueFrom(
      this.http.put<Todo>(`${this.baseUrl}/${id}`, update)
    );
    this.replace(updated);
    return updated;
  }

  async remove(id: string): Promise<void> {
    await firstValueFrom(this.http.delete(`${this.baseUrl}/${id}`));
    this._todos.update(list => list.filter(t => t.id !== id));
  }

  async get(id: string): Promise<Todo> {
    return await firstValueFrom(this.http.get<Todo>(`${this.baseUrl}/${id}`));
  }

  async addSubTask(todoId: string, title: string): Promise<Todo> {
    const updated = await firstValueFrom(
      this.http.post<Todo>(`${this.baseUrl}/${todoId}/subtasks`, { title })
    );
    this.replace(updated);
    return updated;
  }

  async toggleSubTask(todoId: string, subTaskId: string): Promise<Todo> {
    const updated = await firstValueFrom(
      this.http.put<Todo>(`${this.baseUrl}/${todoId}/subtasks/${subTaskId}/toggle`, {})
    );
    this.replace(updated);
    return updated;
  }

  async deleteSubTask(todoId: string, subTaskId: string): Promise<Todo> {
    const updated = await firstValueFrom(
      this.http.delete<Todo>(`${this.baseUrl}/${todoId}/subtasks/${subTaskId}`)
    );
    this.replace(updated);
    return updated;
  }

  private replace(item: Todo): void {
    this._todos.update(list => list.map(t => (t.id === item.id ? item : t)));
  }
}
