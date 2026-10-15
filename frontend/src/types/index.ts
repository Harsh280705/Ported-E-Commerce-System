// Typed DTOs matching the backend API contract (base URL /api)

export interface UserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  role: string
}

export interface AuthResponse {
  accessToken: string
  expiresAt: string
  user: UserDto
}

export interface CategoryDto {
  id: string
  name: string
  slug: string
  description?: string | null
  imageUrl?: string | null
  productCount?: number
}

export interface ProductCardDto {
  id: string
  name: string
  slug: string
  description?: string | null
  price: number
  compareAtPrice?: number | null
  discountPct?: number | null
  stock: number
  inStock: boolean
  imageUrl?: string | null
  categoryId: string
  categoryName?: string | null
}

export interface ProductDetailDto extends ProductCardDto {
  isActive?: boolean
  isFeatured?: boolean
}

export interface ProductPagedResult {
  items: ProductCardDto[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type ProductSort =
  | 'recommended'
  | 'price_asc'
  | 'price_desc'
  | 'name_asc'
  | 'name_desc'
  | 'newest'

export interface CartItemDto {
  productId: string
  productName: string
  slug?: string | null
  imageUrl?: string | null
  quantity: number
  unitPrice: number
  lineTotal: number
  stockAvailable: number
}

export interface CartDto {
  items: CartItemDto[]
  subTotal: number
  shipping: number
  tax: number
  grandTotal: number
  itemCount: number
}

export interface AddressDto {
  id?: string
  fullName: string
  line1: string
  line2?: string | null
  city: string
  state: string
  postalCode: string
  country: string
  phone?: string | null
  isDefault?: boolean
}

export type ShippingMethod = 'standard' | 'express' | 'priority'
export type PaymentMethod = 'cod' | 'card_demo' | 'upi_demo' | 'netbanking_demo'

export interface CreateOrderRequest {
  shippingAddress: AddressDto
  billingAddress?: AddressDto | null
  same?: boolean
  shippingAddressId?: string | null
  billingAddressId?: string | null
  shippingMethod: ShippingMethod
  paymentMethod: PaymentMethod
  idempotencyKey?: string
}

export interface OrderItemDto {
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface OrderDto {
  id: string
  orderNumber: string
  orderDate: string
  status: string
  subTotal: number
  shippingCost: number
  taxAmount: number
  total: number
  shippingMethod: string
  paymentMethod: string
  shippingAddress: AddressDto
  billingAddress?: AddressDto | null
  items: OrderItemDto[]
}

export interface OrderSummaryDto {
  id: string
  orderNumber: string
  orderDate: string
  status: string
  total: number
  itemCount: number
}

export interface AdminDashboardDto {
  totalProducts: number
  totalOrders: number
  totalCustomers: number
  lowStockProducts: number
  outOfStockProducts: number
  pendingOrders: number
  totalRevenue: number
}

export interface AdminProductUpsert {
  name: string
  slug: string
  description?: string | null
  price: number
  compareAtPrice?: number | null
  stock: number
  imageUrl?: string | null
  categoryId: string
  isActive: boolean
  isFeatured: boolean
}

export interface AdminCategoryUpsert {
  name: string
  slug: string
  description?: string | null
  imageUrl?: string | null
  isActive: boolean
  displayOrder: number
}

export interface ToastItem {
  id: string
  message: string
  type: 'success' | 'danger' | 'info' | 'warning'
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}
