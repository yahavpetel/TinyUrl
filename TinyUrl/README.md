# TinyURL

## advantages and disadvantages of LRU:

### Advantages:

* Efficient Memory Usage: LRU (Least Recently Used) caching strategy ensures that frequently accessed items are retained in the cache while less frequently accessed items are evicted, leading to optimal memory usage.
* Improved Performance: By keeping frequently accessed data in memory, LRU caching reduces the need to fetch data from slower storage mediums, resulting in faster response times and improved overall performance.
* Reduced Database Load: With frequently accessed data readily available in the cache, there's a decrease in the number of database queries, reducing the load on the database server and improving scalability.
* Simple Implementation: Implementing LRU caching is relatively straightforward, especially with available libraries or built-in data structures in programming languages.
* Predictable Eviction Policy: LRU caching follows a predictable eviction policy based on the access pattern of items, making it easier to reason about cache behavior and tune cache parameters.


### Disadvantages:

* Memory Overhead: Maintaining both the dictionary and the linked list incurs some memory overhead.
* Complexity: Implementing a custom cache requires careful attention to details and may introduce complexity compared to using existing libraries.
* Potential Performance Overhead: Depending on the implementation, there might be a slight performance overhead compared to highly optimized libraries.