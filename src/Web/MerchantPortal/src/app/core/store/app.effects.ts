import { Injectable } from '@angular/core';
import { Actions, createEffect } from '@ngrx/effects';

@Injectable()
export class AppEffects {
  constructor(private actions$: Actions) {}

  // Add effects here as needed
  // Example:
  // loadData$ = createEffect(() =>
  //   this.actions$.pipe(
  //     ofType(loadData),
  //     switchMap(() =>
  //       this.dataService.loadData().pipe(
  //         map(data => loadDataSuccess({ data })),
  //         catchError(error => of(loadDataFailure({ error })))
  //       )
  //     )
  //   )
  // );
}