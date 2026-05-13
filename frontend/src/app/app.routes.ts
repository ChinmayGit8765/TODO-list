import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'todos' },
  {
    path: 'todos',
    loadComponent: () =>
      import('./pages/todo-list/todo-list').then(m => m.TodoListComponent)
  },
  { path: '**', redirectTo: 'todos' }
];
