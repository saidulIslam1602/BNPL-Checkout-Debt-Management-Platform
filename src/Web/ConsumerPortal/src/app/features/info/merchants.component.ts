import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-merchants',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule
  ],
  template: `
    <div class="merchants-container">
      <div class="hero-section">
        <h1>Our Partner Merchants</h1>
        <p class="subtitle">Shop at thousands of trusted retailers with BNPL</p>
      </div>

      <div class="categories-section">
        <h2>Shop by Category</h2>
        <div class="categories-grid">
          @for (category of categories(); track category.id) {
            <mat-card class="category-card" (click)="selectCategory(category.id)">
              <mat-card-header>
                <mat-icon mat-card-avatar>{{category.icon}}</mat-icon>
                <mat-card-title>{{category.name}}</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <p>{{category.description}}</p>
                <mat-chip-set>
                  @for (tag of category.tags; track tag) {
                    <mat-chip>{{tag}}</mat-chip>
                  }
                </mat-chip-set>
              </mat-card-content>
            </mat-card>
          }
        </div>
      </div>

      <div class="featured-merchants">
        <h2>Featured Merchants</h2>
        <div class="merchants-grid">
          @for (merchant of featuredMerchants(); track merchant.id) {
            <mat-card class="merchant-card">
              <img mat-card-image [src]="merchant.logo" [alt]="merchant.name">
              <mat-card-header>
                <mat-card-title>{{merchant.name}}</mat-card-title>
                <mat-card-subtitle>{{merchant.category}}</mat-card-subtitle>
              </mat-card-header>
              <mat-card-content>
                <p>{{merchant.description}}</p>
                <div class="merchant-stats">
                  <span class="rating">
                    <mat-icon>star</mat-icon>
                    {{merchant.rating}}
                  </span>
                  <span class="offers">{{merchant.activeOffers}} offers</span>
                </div>
              </mat-card-content>
              <mat-card-actions>
                <button mat-button (click)="visitMerchant(merchant.id)">
                  Visit Store
                </button>
              </mat-card-actions>
            </mat-card>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .merchants-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .hero-section {
      text-align: center;
      margin-bottom: 3rem;
      
      h1 {
        font-size: 2.5rem;
        margin-bottom: 1rem;
        color: #1976d2;
      }
      
      .subtitle {
        font-size: 1.2rem;
        color: #666;
      }
    }

    .categories-grid, .merchants-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1.5rem;
      margin-top: 2rem;
    }

    .category-card, .merchant-card {
      cursor: pointer;
      transition: transform 0.2s;
      
      &:hover {
        transform: translateY(-2px);
      }
    }

    .merchant-stats {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-top: 1rem;
      
      .rating {
        display: flex;
        align-items: center;
        gap: 0.25rem;
        color: #ff9800;
      }
      
      .offers {
        color: #4caf50;
        font-weight: 500;
      }
    }

    mat-card-image {
      height: 200px;
      object-fit: cover;
    }
  `]
})
export class MerchantsComponent implements OnInit {
  categories = signal([
    {
      id: 1,
      name: 'Electronics',
      icon: 'devices',
      description: 'Latest gadgets and technology',
      tags: ['Phones', 'Laptops', 'Gaming']
    },
    {
      id: 2,
      name: 'Fashion',
      icon: 'checkroom',
      description: 'Clothing and accessories',
      tags: ['Clothing', 'Shoes', 'Accessories']
    },
    {
      id: 3,
      name: 'Home & Garden',
      icon: 'home',
      description: 'Everything for your home',
      tags: ['Furniture', 'Decor', 'Garden']
    },
    {
      id: 4,
      name: 'Sports & Outdoors',
      icon: 'sports',
      description: 'Gear for active lifestyle',
      tags: ['Fitness', 'Outdoor', 'Sports']
    }
  ]);

  featuredMerchants = signal([
    {
      id: 1,
      name: 'TechNorway',
      category: 'Electronics',
      description: 'Leading electronics retailer in Norway',
      logo: 'assets/merchants/technorway.png',
      rating: 4.8,
      activeOffers: 15
    },
    {
      id: 2,
      name: 'Nordic Fashion',
      category: 'Fashion',
      description: 'Scandinavian fashion and lifestyle',
      logo: 'assets/merchants/nordic-fashion.png',
      rating: 4.6,
      activeOffers: 8
    },
    {
      id: 3,
      name: 'HomeStyle Oslo',
      category: 'Home & Garden',
      description: 'Modern furniture and home decor',
      logo: 'assets/merchants/homestyle.png',
      rating: 4.7,
      activeOffers: 12
    }
  ]);

  ngOnInit() {
    // Load merchants from API
  }

  selectCategory(categoryId: number) {
    console.log('Selected category:', categoryId);
    // TODO: Filter merchants by category
  }

  visitMerchant(merchantId: number) {
    console.log('Visit merchant:', merchantId);
    // TODO: Navigate to merchant store
  }
}