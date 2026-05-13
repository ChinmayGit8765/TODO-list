import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { TodosService } from './todos.service';
import { Todo } from '../models/todo';

function makeTodo(overrides: Partial<Todo> = {}): Todo {
  return {
    id: 'todo-1',
    title: 'Default',
    createdAt: '2026-01-01T00:00:00Z',
    dueAt: null,
    notes: null,
    isComplete: false,
    subTasks: [],
    ...overrides
  };
}

describe('TodosService', () => {
  let service: TodosService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        TodosService
      ]
    });
    service = TestBed.inject(TodosService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('starts with an empty list', () => {
    expect(service.todos()).toEqual([]);
    expect(service.count()).toBe(0);
  });

  it('load() populates todos from GET /api/todos', async () => {
    const promise = service.load();
    const req = httpMock.expectOne('/api/todos');
    expect(req.request.method).toBe('GET');
    req.flush([makeTodo({ id: 'a' }), makeTodo({ id: 'b', title: 'Other' })]);
    await promise;

    expect(service.todos().length).toBe(2);
    expect(service.todos()[1].title).toBe('Other');
    expect(service.count()).toBe(2);
  });

  it('add() POSTs the title and appends the created todo', async () => {
    const promise = service.add('New thing');
    const req = httpMock.expectOne('/api/todos');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ title: 'New thing' });
    const created = makeTodo({ id: 'new', title: 'New thing' });
    req.flush(created);

    const result = await promise;
    expect(result).toEqual(created);
    expect(service.todos()).toEqual([created]);
  });

  it('remove() DELETEs and drops the todo from the local list', async () => {
    // Seed two items first.
    const load = service.load();
    httpMock.expectOne('/api/todos').flush([makeTodo({ id: 'a' }), makeTodo({ id: 'b' })]);
    await load;

    const promise = service.remove('a');
    const req = httpMock.expectOne('/api/todos/a');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;

    expect(service.todos().map(t => t.id)).toEqual(['b']);
  });

  it('update() PUTs and replaces the todo in the list', async () => {
    const load = service.load();
    httpMock.expectOne('/api/todos').flush([makeTodo({ id: 'a' })]);
    await load;

    const promise = service.update('a', {
      title: 'Renamed',
      dueAt: null,
      notes: 'new notes',
      isComplete: true
    });
    const req = httpMock.expectOne('/api/todos/a');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.title).toBe('Renamed');
    const updated = makeTodo({ id: 'a', title: 'Renamed', notes: 'new notes', isComplete: true });
    req.flush(updated);
    await promise;

    expect(service.todos()[0]).toEqual(updated);
  });

  it('addSubTask() POSTs and replaces the parent in the list', async () => {
    const load = service.load();
    httpMock.expectOne('/api/todos').flush([makeTodo({ id: 'a' })]);
    await load;

    const promise = service.addSubTask('a', 'Step 1');
    const req = httpMock.expectOne('/api/todos/a/subtasks');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ title: 'Step 1' });
    const withSub = makeTodo({ id: 'a', subTasks: [{ id: 's1', title: 'Step 1', isComplete: false }] });
    req.flush(withSub);
    await promise;

    expect(service.todos()[0].subTasks).toHaveLength(1);
    expect(service.todos()[0].subTasks[0].title).toBe('Step 1');
  });

  it('toggleSubTask() PUTs to /toggle and replaces parent', async () => {
    const load = service.load();
    httpMock.expectOne('/api/todos').flush([
      makeTodo({ id: 'a', subTasks: [{ id: 's1', title: 'Step 1', isComplete: false }] })
    ]);
    await load;

    const promise = service.toggleSubTask('a', 's1');
    const req = httpMock.expectOne('/api/todos/a/subtasks/s1/toggle');
    expect(req.request.method).toBe('PUT');
    const toggled = makeTodo({ id: 'a', subTasks: [{ id: 's1', title: 'Step 1', isComplete: true }] });
    req.flush(toggled);
    await promise;

    expect(service.todos()[0].subTasks[0].isComplete).toBe(true);
  });

  it('deleteSubTask() DELETEs and replaces parent', async () => {
    const load = service.load();
    httpMock.expectOne('/api/todos').flush([
      makeTodo({ id: 'a', subTasks: [{ id: 's1', title: 'Step 1', isComplete: false }] })
    ]);
    await load;

    const promise = service.deleteSubTask('a', 's1');
    const req = httpMock.expectOne('/api/todos/a/subtasks/s1');
    expect(req.request.method).toBe('DELETE');
    req.flush(makeTodo({ id: 'a', subTasks: [] }));
    await promise;

    expect(service.todos()[0].subTasks).toEqual([]);
  });
});
