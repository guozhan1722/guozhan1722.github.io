// Bind navigator.permissions.query to preserve correct 'this' when passed around
(function(){
  if (typeof navigator !== 'undefined' && navigator.permissions && typeof navigator.permissions.query === 'function') {
    // Create a safe, bound wrapper and expose it on navigator.permissions.__bound_query__
    try {
      navigator.permissions.__bound_query__ = navigator.permissions.query.bind(navigator.permissions);
    } catch (e) {
      console.error('Failed to bind navigator.permissions.query', e);
    }
  }
})();
