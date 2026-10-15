import { defineStore } from 'pinia'
import { apiClient } from '@/api/client'
import type { CategoryDto, ProductCardDto, ProductSort } from '@/types'

interface CatalogState {
  categories: CategoryDto[]
  products: ProductCardDto[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  search: string
  sort: ProductSort
  selectedCategory: string
}

export const useCatalogStore = defineStore('catalog', {
  state: (): CatalogState => ({
    categories: [],
    products: [],
    page: 1,
    pageSize: 12,
    totalCount: 0,
    totalPages: 0,
    search: '',
    sort: 'recommended',
    selectedCategory: ''
  }),
  actions: {
    async fetchCategories(): Promise<void> {
      const res = await apiClient.get<CategoryDto[]>('/categories')
      this.categories = res.data ?? []
    },
    async fetchProducts(overrides?: {
      page?: number
      pageSize?: number
      search?: string
      sort?: ProductSort
      categoryId?: string
    }): Promise<void> {
      const page = overrides?.page ?? this.page
      const pageSize = overrides?.pageSize ?? this.pageSize
      const search = overrides?.search ?? this.search
      const sort = overrides?.sort ?? this.sort
      const categoryId = overrides?.categoryId ?? this.selectedCategory
      const params: Record<string, string | number> = { page, pageSize }
      if (categoryId) params.categoryId = categoryId
      if (search) params.search = search
      if (sort) params.sort = sort
      const res = await apiClient.get<{
        items: ProductCardDto[]
        page: number
        pageSize: number
        totalCount: number
        totalPages: number
      }>('/products', { params })
      this.products = res.data.items ?? []
      this.page = res.data.page ?? page
      this.pageSize = res.data.pageSize ?? pageSize
      this.totalCount = res.data.totalCount ?? 0
      this.totalPages = res.data.totalPages ?? 0
      this.search = search
      this.sort = sort
      this.selectedCategory = categoryId
    },
    setSearch(v: string) {
      this.search = v
    },
    setSort(v: ProductSort) {
      this.sort = v
    },
    setCategory(v: string) {
      this.selectedCategory = v
    },
    setPage(v: number) {
      this.page = v
    }
  }
})
