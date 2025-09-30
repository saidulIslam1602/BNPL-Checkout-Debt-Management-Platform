import { ActionReducerMap, MetaReducer } from '@ngrx/store';
import { environment } from '../../../environments/environment';

// Define the app state interface
export interface AppState {
  // Add state slices here as needed
  // auth: AuthState;
  // transactions: TransactionState;
  // analytics: AnalyticsState;
}

// Define reducers
export const reducers: ActionReducerMap<AppState> = {
  // Add reducers here as needed
  // auth: authReducer,
  // transactions: transactionReducer,
  // analytics: analyticsReducer,
};

// Meta reducers
export const metaReducers: MetaReducer<AppState>[] = !environment.production 
  ? [] 
  : [];

// Selectors can be added here
// export const selectAuthState = (state: AppState) => state.auth;
// export const selectTransactionState = (state: AppState) => state.transactions;